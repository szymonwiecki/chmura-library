/* ── Config ────────────────────────────────────────────────────── */
const API_BASE = window.location.origin;

const BOOK_TYPES = [
  { label: 'Physical',  icon: '📖', cls: 'type-physical'  },
  { label: 'Ebook',     icon: '💻', cls: 'type-ebook'     },
  { label: 'Audiobook', icon: '🎧', cls: 'type-audiobook' },
];

let token = '';
let pendingDeleteId = null;
let allBooks = [];
let showFavoritesOnly = false;

/* ── Toast ─────────────────────────────────────────────────────── */
function toast(message, type = 'success', duration = 3500) {
  const container = document.getElementById('toast-container');
  const el = document.createElement('div');
  el.className = `toast ${type}`;
  el.textContent = message;
  container.appendChild(el);
  setTimeout(() => {
    el.classList.add('fade-out');
    el.addEventListener('animationend', () => el.remove());
  }, duration);
}

/* ── Modal ─────────────────────────────────────────────────────── */
function openModal(id) {
  document.getElementById(id).classList.remove('hidden');
  document.body.style.overflow = 'hidden';
}
function closeModal(id) {
  document.getElementById(id).classList.add('hidden');
  document.body.style.overflow = '';
}

document.addEventListener('click', e => {
  const target = e.target.closest('[data-close]');
  if (target) closeModal(target.dataset.close);
});
document.addEventListener('keydown', e => {
  if (e.key === 'Escape') {
    document.querySelectorAll('.modal:not(.hidden)').forEach(m => closeModal(m.id));
  }
});

/* ── API helpers ───────────────────────────────────────────────── */
async function apiFetch(method, path, body = null) {
  const opts = {
    method,
    headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
  };
  if (body) opts.body = JSON.stringify(body);
  const res = await fetch(`${API_BASE}${path}`, opts);
  return res;
}

async function apiJson(method, path, body = null) {
  const res = await apiFetch(method, path, body);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.details || err.message || `HTTP ${res.status}`);
  }
  if (res.status === 204) return null;
  return res.json();
}

/* ── Auth ──────────────────────────────────────────────────────── */
function showApp(username) {
  document.getElementById('login-section').classList.add('hidden');
  document.getElementById('main-section').classList.remove('hidden');
  document.getElementById('nav-username').textContent = username;
  document.getElementById('logout-btn').classList.remove('hidden');
  loadBooks();
}

function showLogin() {
  token = '';
  document.getElementById('main-section').classList.add('hidden');
  document.getElementById('login-section').classList.remove('hidden');
  document.getElementById('nav-username').textContent = '';
  document.getElementById('logout-btn').classList.add('hidden');
}

document.getElementById('logout-btn').addEventListener('click', () => {
  showLogin();
  toast('Logged out', 'info');
});

document.getElementById('login-form').addEventListener('submit', async e => {
  e.preventDefault();
  const btn = document.getElementById('login-btn');
  const label = document.getElementById('login-label');
  const spinner = document.getElementById('login-spinner');

  btn.disabled = true;
  label.classList.add('hidden');
  spinner.classList.remove('hidden');

  const username = document.getElementById('username').value;
  const password = document.getElementById('password').value;

  try {
    const res = await fetch(`${API_BASE}/api/Auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password }),
    });
    if (!res.ok) throw new Error('Invalid credentials');
    const data = await res.json();
    token = data.token;
    toast(`Welcome, ${username}!`, 'success');
    showApp(username);
  } catch (err) {
    toast(err.message || 'Login failed', 'error');
  } finally {
    btn.disabled = false;
    label.classList.remove('hidden');
    spinner.classList.add('hidden');
  }
});

/* ── Filter ────────────────────────────────────────────────────── */
document.getElementById('filter-all-btn').addEventListener('click', () => {
  showFavoritesOnly = false;
  document.getElementById('filter-all-btn').classList.add('active');
  document.getElementById('filter-fav-btn').classList.remove('active');
  renderBooks();
});

document.getElementById('filter-fav-btn').addEventListener('click', () => {
  showFavoritesOnly = true;
  document.getElementById('filter-fav-btn').classList.add('active');
  document.getElementById('filter-all-btn').classList.remove('active');
  renderBooks();
});

/* ── Books ─────────────────────────────────────────────────────── */
async function loadBooks() {
  const grid = document.getElementById('books-grid');
  const loading = document.getElementById('loading-grid');
  const empty = document.getElementById('empty-state');

  loading.classList.remove('hidden');
  empty.classList.add('hidden');
  document.querySelectorAll('.book-card').forEach(c => c.remove());

  try {
    allBooks = await apiJson('GET', '/api/Books');
    loading.classList.add('hidden');
    renderBooks();
  } catch (err) {
    loading.classList.add('hidden');
    toast('Failed to load books: ' + err.message, 'error');
  }
}

function renderBooks() {
  const grid = document.getElementById('books-grid');
  const empty = document.getElementById('empty-state');
  document.querySelectorAll('.book-card').forEach(c => c.remove());

  const books = showFavoritesOnly ? allBooks.filter(b => b.isFavorite) : allBooks;
  document.getElementById('book-count').textContent = books.length;

  if (books.length === 0) {
    empty.classList.remove('hidden');
    return;
  }
  empty.classList.add('hidden');
  books.forEach(book => grid.appendChild(buildBookCard(book)));
}

function getTypeIndex(bookType) {
  if (typeof bookType === 'string') {
    const idx = BOOK_TYPES.findIndex(t => t.label === bookType);
    return idx >= 0 ? idx : 0;
  }
  return typeof bookType === 'number' ? bookType : 0;
}

function buildBookCard(book) {
  const typeIdx = getTypeIndex(book.bookType);
  const type = BOOK_TYPES[typeIdx] || BOOK_TYPES[0];

  const card = document.createElement('div');
  card.className = 'book-card';
  card.dataset.id = book.id;

  const coverHTML = book.coverImageUrl
    ? `<img src="${escapeHtml(book.coverImageUrl)}" alt="Cover" loading="lazy">`
    : `<span class="book-cover-placeholder">${type.icon}</span>`;

  card.innerHTML = `
    <div class="book-cover">
      ${coverHTML}
      <button class="star-btn ${book.isFavorite ? 'starred' : ''}" title="${book.isFavorite ? 'Remove from favorites' : 'Add to favorites'}">
        ${book.isFavorite ? '⭐' : '☆'}
      </button>
    </div>
    <div class="book-body">
      <span class="book-type-badge ${type.cls}">${type.icon} ${type.label}</span>
      <h3 class="book-title">${escapeHtml(book.title)}</h3>
      <p class="book-author">${escapeHtml(book.author)}</p>
      <div class="book-meta">
        <span>📅 ${book.publishedYear}</span>
        <span>🏷 ${escapeHtml(book.genre)}</span>
      </div>
      ${book.description ? `<p class="book-description">${escapeHtml(book.description)}</p>` : ''}
      ${book.notes ? `<div class="book-notes-badge" title="${escapeHtml(book.notes)}">📝 Has notes</div>` : ''}
    </div>
    <div class="book-actions">
      <button class="btn btn-sm btn-secondary btn-edit" data-id="${book.id}">✏ Edit</button>
      <button class="btn btn-sm btn-danger btn-delete" data-id="${book.id}" data-title="${escapeHtml(book.title)}">🗑 Delete</button>
    </div>`;

  card.querySelector('.book-body').addEventListener('click', () => openDetailModal(book));
  card.querySelector('.star-btn').addEventListener('click', e => {
    e.stopPropagation();
    toggleFavorite(book, card.querySelector('.star-btn'));
  });
  card.querySelector('.btn-edit').addEventListener('click', e => {
    e.stopPropagation();
    openEditModal(book);
  });
  card.querySelector('.btn-delete').addEventListener('click', e => {
    e.stopPropagation();
    confirmDelete(book.id, book.title);
  });

  return card;
}

function escapeHtml(str) {
  return String(str)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

/* ── Favorite toggle ───────────────────────────────────────────── */
async function toggleFavorite(book, btn) {
  try {
    await apiJson('PATCH', `/api/Books/${book.id}/favorite`);
    book.isFavorite = !book.isFavorite;
    btn.textContent = book.isFavorite ? '⭐' : '☆';
    btn.classList.toggle('starred', book.isFavorite);
    btn.title = book.isFavorite ? 'Remove from favorites' : 'Add to favorites';
    if (showFavoritesOnly) renderBooks();
  } catch (err) {
    toast('Failed to update favorite: ' + err.message, 'error');
  }
}

/* ── Book Detail Modal ─────────────────────────────────────────── */
function openDetailModal(book) {
  const typeIdx = getTypeIndex(book.bookType);
  const type = BOOK_TYPES[typeIdx] || BOOK_TYPES[0];

  document.getElementById('detail-modal-title').textContent = book.title;

  const coverEl = document.getElementById('detail-cover');
  if (book.coverImageUrl) {
    coverEl.innerHTML = `<img src="${escapeHtml(book.coverImageUrl)}" alt="Cover">`;
  } else {
    coverEl.innerHTML = `<span class="detail-cover-icon">${type.icon}</span>`;
  }

  const badgesEl = document.getElementById('detail-badges');
  badgesEl.innerHTML =
    `<span class="book-type-badge ${type.cls}">${type.icon} ${type.label}</span>` +
    (book.isFavorite ? '<span class="fav-badge">⭐ Favorite</span>' : '');

  document.getElementById('detail-book-title').textContent = book.title;
  document.getElementById('detail-author').textContent = book.author;
  document.getElementById('detail-meta').innerHTML =
    `<span>📅 ${book.publishedYear}</span><span>🏷 ${escapeHtml(book.genre)}</span>`;

  const descEl = document.getElementById('detail-description');
  if (book.description) {
    descEl.textContent = book.description;
    descEl.classList.remove('hidden');
  } else {
    descEl.classList.add('hidden');
  }

  const notesEl = document.getElementById('detail-notes');
  if (book.notes) {
    notesEl.innerHTML =
      `<div class="detail-notes-label">📝 My notes</div>` +
      `<div class="detail-notes-text">${escapeHtml(book.notes)}</div>`;
    notesEl.classList.remove('hidden');
  } else {
    notesEl.classList.add('hidden');
  }

  document.getElementById('detail-edit-btn').onclick = () => {
    closeModal('detail-modal');
    openEditModal(book);
  };

  openModal('detail-modal');
}

/* ── Cover upload preview helper ───────────────────────────────── */
function initCoverInput(inputId, previewId, placeholderId) {
  document.getElementById(inputId).addEventListener('change', e => {
    const file = e.target.files[0];
    if (file) showCoverPreview(previewId, placeholderId, URL.createObjectURL(file));
    else       resetCoverPreview(previewId, placeholderId);
  });
}

function showCoverPreview(previewId, placeholderId, src) {
  const preview = document.getElementById(previewId);
  const placeholder = document.getElementById(placeholderId);
  preview.src = src;
  preview.classList.remove('hidden');
  placeholder.classList.add('hidden');
}

function resetCoverPreview(previewId, placeholderId) {
  const preview = document.getElementById(previewId);
  const placeholder = document.getElementById(placeholderId);
  preview.src = '';
  preview.classList.add('hidden');
  placeholder.classList.remove('hidden');
}

initCoverInput('add-cover',  'add-cover-preview',  'add-cover-placeholder');
initCoverInput('edit-cover', 'edit-cover-preview', 'edit-cover-placeholder');

/* ── AI Description Generator ──────────────────────────────────── */
async function generateDescription(titleId, authorId, yearId, genreId, targetId, btnId) {
  const title  = document.getElementById(titleId).value.trim();
  const author = document.getElementById(authorId).value.trim();
  const year   = parseInt(document.getElementById(yearId).value) || 0;
  const genre  = document.getElementById(genreId).value.trim();

  if (!title || !author || !genre) {
    toast('Fill in Title, Author and Genre first', 'info');
    return;
  }

  const btn = document.getElementById(btnId);
  const originalText = btn.textContent;
  btn.disabled = true;
  btn.textContent = '⏳ Generating…';

  try {
    const data = await apiJson('POST', '/api/Books/generate-description',
      { title, author, genre, year });
    document.getElementById(targetId).value = data.description;
    toast('Description generated!', 'success');
  } catch (err) {
    toast('AI generation failed: ' + err.message, 'error');
  } finally {
    btn.disabled = false;
    btn.textContent = originalText;
  }
}

document.getElementById('add-generate-btn').addEventListener('click', () =>
  generateDescription('add-title', 'add-author', 'add-year', 'add-genre', 'add-description', 'add-generate-btn'));

document.getElementById('edit-generate-btn').addEventListener('click', () =>
  generateDescription('edit-title', 'edit-author', 'edit-year', 'edit-genre', 'edit-description', 'edit-generate-btn'));

/* ── Google Books Search ────────────────────────────────────────── */
let searchTimer = null;

const searchInput    = document.getElementById('google-search-input');
const searchDropdown = document.getElementById('search-dropdown');
const searchClearBtn = document.getElementById('search-clear-btn');

function closeSearchDropdown() {
  searchDropdown.classList.add('hidden');
}

searchInput.addEventListener('input', () => {
  const q = searchInput.value.trim();
  searchClearBtn.classList.toggle('hidden', !q);
  clearTimeout(searchTimer);
  if (!q) { closeSearchDropdown(); return; }
  searchTimer = setTimeout(() => runGoogleSearch(q), 350);
});

searchClearBtn.addEventListener('click', () => {
  searchInput.value = '';
  searchClearBtn.classList.add('hidden');
  closeSearchDropdown();
  searchInput.focus();
});

document.addEventListener('click', e => {
  if (!e.target.closest('.search-wrapper')) closeSearchDropdown();
});
searchInput.addEventListener('focus', () => {
  if (searchInput.value.trim() && !searchDropdown.classList.contains('hidden')) return;
  if (searchInput.value.trim()) runGoogleSearch(searchInput.value.trim());
});

async function runGoogleSearch(q) {
  searchDropdown.innerHTML = '<div class="search-loading">🔍 Searching…</div>';
  searchDropdown.classList.remove('hidden');

  try {
    const books = await apiJson('GET', `/api/googlebooks/search?q=${encodeURIComponent(q)}`);
    renderSearchResults(books);
  } catch (err) {
    searchDropdown.innerHTML = `<div class="search-empty">Search failed: ${escapeHtml(err.message)}</div>`;
  }
}

function renderSearchResults(books) {
  if (!books || books.length === 0) {
    searchDropdown.innerHTML = '<div class="search-empty">No results found.</div>';
    return;
  }

  searchDropdown.innerHTML = books.map((b, i) => {
    const thumb = b.thumbnailUrl
      ? `<img src="${escapeHtml(b.thumbnailUrl)}" alt="Cover" loading="lazy">`
      : '📚';
    return `
      <div class="search-result-item">
        <div class="search-result-thumb">${thumb}</div>
        <div class="search-result-info">
          <div class="search-result-title" title="${escapeHtml(b.title)}">${escapeHtml(b.title)}</div>
          <div class="search-result-meta">
            ${escapeHtml(b.author || 'Unknown')}
            ${b.publishedYear ? ` · ${b.publishedYear}` : ''}
            ${b.genre ? ` · ${escapeHtml(b.genre)}` : ''}
          </div>
        </div>
        <button class="btn btn-sm btn-primary search-result-add" data-idx="${i}">+ Add</button>
      </div>`;
  }).join('');

  searchDropdown._results = books;

  searchDropdown.querySelectorAll('.search-result-add').forEach(btn => {
    btn.addEventListener('click', () => {
      const book = searchDropdown._results[parseInt(btn.dataset.idx)];
      prefillAddModal(book);
      closeSearchDropdown();
      searchInput.value = '';
      searchClearBtn.classList.add('hidden');
    });
  });
}

function prefillAddModal(book) {
  document.getElementById('add-book-form').reset();
  document.getElementById('add-title').value       = book.title || '';
  document.getElementById('add-author').value      = book.author || '';
  document.getElementById('add-year').value        = book.publishedYear || '';
  document.getElementById('add-genre').value       = book.genre || '';
  document.getElementById('add-description').value = book.description || '';
  document.getElementById('add-notes').value       = '';
  document.getElementById('add-type').value        = '0';
  document.getElementById('add-google-thumbnail').value = book.thumbnailUrl || '';
  resetCoverPreview('add-cover-preview', 'add-cover-placeholder');

  // Show thumbnail as cover preview if available
  if (book.thumbnailUrl) {
    showCoverPreview('add-cover-preview', 'add-cover-placeholder', book.thumbnailUrl);
  }

  openModal('add-modal');
}

/* ── Upload cover helper ────────────────────────────────────────── */
async function uploadCover(bookId, file) {
  const form = new FormData();
  form.append('file', file);
  const res = await fetch(`${API_BASE}/api/books/${bookId}/cover`, {
    method: 'POST',
    headers: { 'Authorization': `Bearer ${token}` },
    body: form,
  });
  if (!res.ok) {
    const data = await res.json().catch(() => ({}));
    throw new Error(data.message || `HTTP ${res.status}`);
  }
}

/* ── Add Book ──────────────────────────────────────────────────── */
function openAddModal() {
  document.getElementById('add-book-form').reset();
  document.getElementById('add-description').value = '';
  document.getElementById('add-notes').value = '';
  document.getElementById('add-google-thumbnail').value = '';
  resetCoverPreview('add-cover-preview', 'add-cover-placeholder');
  openModal('add-modal');
}

document.getElementById('open-add-btn').addEventListener('click', openAddModal);
document.getElementById('empty-add-btn').addEventListener('click', openAddModal);

document.getElementById('add-book-form').addEventListener('submit', async e => {
  e.preventDefault();
  const btn = e.target.querySelector('[type="submit"]');
  btn.disabled = true;

  const coverFile = document.getElementById('add-cover').files[0];
  const googleThumbnail = document.getElementById('add-google-thumbnail').value;

  const payload = {
    title:         document.getElementById('add-title').value,
    author:        document.getElementById('add-author').value,
    publishedYear: parseInt(document.getElementById('add-year').value),
    genre:         document.getElementById('add-genre').value,
    bookType:      parseInt(document.getElementById('add-type').value),
    description:   document.getElementById('add-description').value.trim() || null,
    notes:         document.getElementById('add-notes').value.trim() || null,
    // Use Google thumbnail as initial cover (will be overwritten if file is selected)
    coverImageUrl: !coverFile && googleThumbnail ? googleThumbnail : null,
  };

  let newBook;
  try {
    newBook = await apiJson('POST', '/api/Books', payload);
  } catch (err) {
    toast('Error: ' + err.message, 'error');
    btn.disabled = false;
    return;
  }

  // If user also selected a file, upload it (overrides Google thumbnail)
  if (coverFile && newBook?.id) {
    try {
      await uploadCover(newBook.id, coverFile);
      toast('Cover uploaded!', 'success');
    } catch (err) {
      toast('Book added but cover upload failed: ' + err.message, 'info');
    }
  }

  closeModal('add-modal');
  toast(`"${payload.title}" added!`, 'success');
  loadBooks();
  btn.disabled = false;
});

/* ── Edit Book ─────────────────────────────────────────────────── */
function openEditModal(book) {
  document.getElementById('edit-id').value = book.id;
  document.getElementById('edit-title').value = book.title;
  document.getElementById('edit-author').value = book.author;
  document.getElementById('edit-year').value = book.publishedYear;
  document.getElementById('edit-genre').value = book.genre;
  document.getElementById('edit-type').value = getTypeIndex(book.bookType);
  document.getElementById('edit-description').value = book.description || '';
  document.getElementById('edit-notes').value = book.notes || '';
  document.getElementById('edit-cover').value = '';

  if (book.coverImageUrl) {
    showCoverPreview('edit-cover-preview', 'edit-cover-placeholder', book.coverImageUrl);
  } else {
    resetCoverPreview('edit-cover-preview', 'edit-cover-placeholder');
  }

  openModal('edit-modal');
}

document.getElementById('edit-book-form').addEventListener('submit', async e => {
  e.preventDefault();
  const btn = e.target.querySelector('[type="submit"]');
  btn.disabled = true;

  const id = parseInt(document.getElementById('edit-id').value);
  const payload = {
    id,
    title:         document.getElementById('edit-title').value,
    author:        document.getElementById('edit-author').value,
    publishedYear: parseInt(document.getElementById('edit-year').value),
    genre:         document.getElementById('edit-genre').value,
    bookType:      parseInt(document.getElementById('edit-type').value),
    description:   document.getElementById('edit-description').value.trim() || null,
    notes:         document.getElementById('edit-notes').value.trim() || null,
  };

  try {
    await apiJson('PUT', `/api/Books/${id}`, payload);
  } catch (err) {
    toast('Failed to update book: ' + err.message, 'error');
    btn.disabled = false;
    return;
  }

  const coverFile = document.getElementById('edit-cover').files[0];
  if (coverFile) {
    try {
      await uploadCover(id, coverFile);
      toast('Cover uploaded!', 'success');
    } catch (err) {
      toast('Cover upload failed: ' + err.message, 'info');
    }
  }

  closeModal('edit-modal');
  toast(`"${payload.title}" updated!`, 'success');
  loadBooks();
  btn.disabled = false;
});

/* ── Delete Book ───────────────────────────────────────────────── */
function confirmDelete(id, title) {
  pendingDeleteId = id;
  document.getElementById('confirm-text').textContent =
    `Are you sure you want to delete "${title}"? This action cannot be undone.`;
  openModal('confirm-modal');
}

document.getElementById('confirm-delete-btn').addEventListener('click', async () => {
  if (!pendingDeleteId) return;
  const btn = document.getElementById('confirm-delete-btn');
  btn.disabled = true;

  try {
    await apiJson('DELETE', `/api/Books/${pendingDeleteId}`);
    closeModal('confirm-modal');
    toast('Book deleted', 'success');
    loadBooks();
  } catch (err) {
    toast('Error: ' + err.message, 'error');
  } finally {
    btn.disabled = false;
    pendingDeleteId = null;
  }
});

/* ── Refresh ───────────────────────────────────────────────────── */
document.getElementById('refresh-btn').addEventListener('click', () => {
  loadBooks();
  toast('Refreshed', 'info', 1500);
});

/* ── Command History ───────────────────────────────────────────── */
document.getElementById('history-btn').addEventListener('click', async () => {
  const content = document.getElementById('history-content');
  content.innerHTML = '<p class="loading-text">Loading...</p>';
  openModal('history-modal');

  try {
    const items = await apiJson('GET', '/api/Books/history');

    if (!items || items.length === 0) {
      content.innerHTML = '<p class="history-empty">No commands in history yet.</p>';
      return;
    }

    const rows = items.map(item => {
      const dt = new Date(item.executedAt);
      const timeStr = dt.toLocaleDateString() + ' ' + dt.toLocaleTimeString();
      return `<tr>
        <td>${escapeHtml(item.description)}</td>
        <td class="history-time">${timeStr}</td>
      </tr>`;
    }).join('');

    content.innerHTML = `
      <table class="history-table">
        <thead>
          <tr><th>Command</th><th>Executed At</th></tr>
        </thead>
        <tbody>${rows}</tbody>
      </table>`;
  } catch (err) {
    content.innerHTML = `<p class="history-empty">Failed to load history: ${escapeHtml(err.message)}</p>`;
  }
});
