/* ================================
   Community Page JavaScript
================================ */
(function () {
    'use strict';

    // ── State ──────────────────────────────────────────────────────────
    let currentPage = 1;
    let currentCategory = 'All';
    let currentSearch = '';
    let totalPages = 1;
    let searchTimer = null;
    let antiforgeryToken = '';
    let totalPostCount = 0;
    let totalCommentCount = 0;

    // ── DOM Refs ───────────────────────────────────────────────────────
    const feed = document.getElementById('cmFeed');
    const paginationWrap = document.getElementById('cmPagination');
    const createModal = document.getElementById('cmCreateModal');
    const newPostBtn = document.getElementById('cmNewPostBtn');
    const modalCloseBtn = document.getElementById('cmModalClose');
    const createForm = document.getElementById('cmCreateForm');
    const searchInput = document.getElementById('cmSearchInput');
    const categoryBtns = document.querySelectorAll('.cm-cat-btn');
    const totalPostsEl = document.getElementById('cmTotalPosts');
    const totalCommentsEl = document.getElementById('cmTotalComments');
    const imageInput = document.getElementById('cmPostImage');
    const imageBtn = document.getElementById('cmImageBtn');
    const imagePreview = document.getElementById('cmImagePreview');
    const imagePreviewImg = document.getElementById('cmImagePreviewImg');
    const imageRemoveBtn = document.getElementById('cmImageRemove');
    const MAX_IMAGE_BYTES = 5 * 1024 * 1024;

    // ── Init ───────────────────────────────────────────────────────────
    function init() {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        if (tokenInput) antiforgeryToken = tokenInput.value;

        loadPosts();

        newPostBtn?.addEventListener('click', openModal);
        modalCloseBtn?.addEventListener('click', closeModal);
        createModal?.addEventListener('click', e => {
            if (e.target === createModal) closeModal();
        });

        createForm?.addEventListener('submit', handleCreatePost);

        searchInput?.addEventListener('input', () => {
            clearTimeout(searchTimer);
            searchTimer = setTimeout(() => {
                currentSearch = searchInput.value.trim();
                currentPage = 1;
                loadPosts();
            }, 350);
        });

        categoryBtns.forEach(btn => {
            btn.addEventListener('click', () => {
                categoryBtns.forEach(b => b.classList.remove('active'));
                btn.classList.add('active');
                currentCategory = btn.dataset.category;
                currentPage = 1;
                loadPosts();
            });
        });

        // Char counter for textarea
        const titleInput = document.getElementById('cmPostTitle');
        const contentArea = document.getElementById('cmPostContent');
        const titleCount = document.getElementById('cmTitleCount');
        const contentCount = document.getElementById('cmContentCount');
        titleInput?.addEventListener('input', () => {
            if (titleCount) titleCount.textContent = titleInput.value.length + ' / 150';
        });
        contentArea?.addEventListener('input', () => {
            if (contentCount) contentCount.textContent = contentArea.value.length + ' / 2000';
        });

        // Image picker
        imageBtn?.addEventListener('click', () => imageInput?.click());
        imageInput?.addEventListener('change', handleImageSelect);
        imageRemoveBtn?.addEventListener('click', clearImageSelection);
    }

    // ── Image Selection ────────────────────────────────────────────────
    function handleImageSelect() {
        const file = imageInput?.files?.[0];
        if (!file) { clearImageSelection(); return; }

        if (!file.type.startsWith('image/')) {
            showToast('Please select an image file.', 'error');
            clearImageSelection();
            return;
        }
        if (file.size > MAX_IMAGE_BYTES) {
            showToast('Image exceeds the 5 MB limit.', 'error');
            clearImageSelection();
            return;
        }

        const reader = new FileReader();
        reader.onload = e => {
            if (imagePreviewImg) imagePreviewImg.src = e.target.result;
            if (imagePreview) imagePreview.hidden = false;
            if (imageBtn) imageBtn.hidden = true;
        };
        reader.readAsDataURL(file);
    }

    function clearImageSelection() {
        if (imageInput) imageInput.value = '';
        if (imagePreviewImg) imagePreviewImg.src = '';
        if (imagePreview) imagePreview.hidden = true;
        if (imageBtn) imageBtn.hidden = false;
    }

    // ── Modal ──────────────────────────────────────────────────────────
    function openModal() {
        createModal?.classList.add('open');
        document.body.style.overflow = 'hidden';
        document.getElementById('cmPostTitle')?.focus();
    }

    function closeModal() {
        createModal?.classList.remove('open');
        document.body.style.overflow = '';
        createForm?.reset();
        clearImageSelection();
        const titleCount = document.getElementById('cmTitleCount');
        const contentCount = document.getElementById('cmContentCount');
        if (titleCount) titleCount.textContent = '0 / 150';
        if (contentCount) contentCount.textContent = '0 / 2000';
    }

    // ── Load Posts ─────────────────────────────────────────────────────
    function loadPosts() {
        showSkeletons();
        const params = new URLSearchParams({
            page: currentPage,
            category: currentCategory === 'All' ? '' : currentCategory,
            search: currentSearch
        });

        fetch('/Community/GetPosts?' + params.toString(), {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .then(r => r.json())
            .then(data => {
                if (!data.success) { showError(); return; }
                totalPages = data.totalPages;
                totalPostCount = data.totalCount;
                renderPosts(data.posts);
                renderPagination();
                updateStats(data.posts);
            })
            .catch(showError);
    }

    function updateStats(posts) {
        if (totalPostsEl) totalPostsEl.textContent = totalPostCount;
    }

    // ── Render Posts ───────────────────────────────────────────────────
    function renderPosts(posts) {
        if (!feed) return;
        if (!posts || posts.length === 0) {
            feed.innerHTML = emptyStateHtml();
            if (paginationWrap) paginationWrap.innerHTML = '';
            return;
        }
        feed.innerHTML = posts.map(p => postCardHtml(p)).join('');
        bindPostCardEvents();
    }

    function emptyStateHtml() {
        return `
<div class="cm-empty-state">
    <div class="cm-empty-icon"><i class="fas fa-comments"></i></div>
    <h4 class="cm-empty-title">No posts yet</h4>
    <p class="cm-empty-sub">Be the first to share something with the community!</p>
</div>`;
    }

    function postCardHtml(p) {
        const timeAgo = formatTimeAgo(p.createdAt);
        const avatarUrl = `https://ui-avatars.com/api/?name=${encodeURIComponent(p.authorName || 'U')}&background=1baebe&color=fff&size=80`;
        const deleteBtn = p.isMyPost
            ? `<button class="cm-post-delete-btn" data-post-id="${p.communityPostId}" title="Delete post">
                <i class="fas fa-trash"></i>
               </button>`
            : '';
        const likeClass = p.isLikedByMe ? 'liked' : '';
        const likeIcon = p.isLikedByMe ? 'fas' : 'far';

        return `
<div class="cm-post-card" id="post-${p.communityPostId}">
    <div class="cm-post-header">
        <div class="cm-post-author-row">
            <img class="cm-post-avatar" src="${avatarUrl}" alt="${escHtml(p.authorName)}" />
            <div>
                <div class="cm-post-author-name">${escHtml(p.authorName)}</div>
                <div class="cm-post-meta">
                    <i class="far fa-clock"></i>
                    <span>${timeAgo}</span>
                    <span>&middot;</span>
                    <span class="cm-post-category-badge">${escHtml(p.category)}</span>
                </div>
            </div>
        </div>
        <div class="cm-post-actions-top">
            ${deleteBtn}
        </div>
    </div>
    <div class="cm-post-body">
        <h4 class="cm-post-title">${escHtml(p.title)}</h4>
        <p class="cm-post-content">${escHtml(p.content)}</p>
        ${p.imageUrl ? `<div class="cm-post-image-wrap"><img class="cm-post-image" src="${escHtml(p.imageUrl)}" alt="Post photo" loading="lazy" /></div>` : ''}
    </div>
    <div class="cm-post-footer">
        <button class="cm-action-btn cm-like-btn ${likeClass}" data-post-id="${p.communityPostId}">
            <i class="${likeIcon} fa-heart"></i>
            <span class="cm-action-count">${p.likeCount}</span>
            <span>${p.likeCount === 1 ? 'Like' : 'Likes'}</span>
        </button>
        <button class="cm-action-btn cm-toggle-comments-btn" data-post-id="${p.communityPostId}">
            <i class="far fa-comment"></i>
            <span class="cm-action-count" id="cc-${p.communityPostId}">${p.commentCount}</span>
            <span>${p.commentCount === 1 ? 'Comment' : 'Comments'}</span>
        </button>
    </div>
    <div class="cm-comments-section" id="comments-${p.communityPostId}">
        <div class="cm-comments-list" id="comments-list-${p.communityPostId}"></div>
        <div class="cm-comment-input-row">
            <div class="cm-comment-input-wrap">
                <textarea class="cm-comment-textarea" placeholder="Write a comment…" rows="1"
                    data-post-id="${p.communityPostId}" maxlength="1000"></textarea>
            </div>
            <button class="cm-comment-send-btn" data-post-id="${p.communityPostId}" title="Send">
                <i class="fas fa-paper-plane"></i>
            </button>
        </div>
    </div>
</div>`;
    }

    // ── Bind Events ────────────────────────────────────────────────────
    function bindPostCardEvents() {
        // Like buttons
        document.querySelectorAll('.cm-like-btn').forEach(btn => {
            btn.addEventListener('click', () => toggleLike(btn));
        });

        // Toggle comments
        document.querySelectorAll('.cm-toggle-comments-btn').forEach(btn => {
            btn.addEventListener('click', () => toggleComments(btn.dataset.postId));
        });

        // Delete post buttons
        document.querySelectorAll('.cm-post-delete-btn').forEach(btn => {
            btn.addEventListener('click', () => deletePost(btn.dataset.postId));
        });

        // Comment send buttons
        document.querySelectorAll('.cm-comment-send-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                const postId = btn.dataset.postId;
                const textarea = document.querySelector(`.cm-comment-textarea[data-post-id="${postId}"]`);
                if (textarea) submitComment(postId, textarea);
            });
        });

        // Comment textarea enter key (Shift+Enter for newline)
        document.querySelectorAll('.cm-comment-textarea').forEach(ta => {
            ta.addEventListener('keydown', e => {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    const postId = ta.dataset.postId;
                    submitComment(postId, ta);
                }
            });
        });
    }

    // ── Like ───────────────────────────────────────────────────────────
    function toggleLike(btn) {
        const postId = btn.dataset.postId;
        btn.disabled = true;

        const fd = new FormData();
        fd.append('postId', postId);
        fd.append('__RequestVerificationToken', antiforgeryToken);

        fetch('/Community/ToggleLike', { method: 'POST', body: fd })
            .then(r => r.json())
            .then(data => {
                if (!data.success) { btn.disabled = false; return; }
                const countEl = btn.querySelector('.cm-action-count');
                const labelEl = btn.querySelectorAll('span')[1];
                const icon = btn.querySelector('i');

                if (data.liked) {
                    btn.classList.add('liked');
                    icon.className = 'fas fa-heart';
                } else {
                    btn.classList.remove('liked');
                    icon.className = 'far fa-heart';
                }
                if (countEl) countEl.textContent = data.likeCount;
                if (labelEl) labelEl.textContent = data.likeCount === 1 ? 'Like' : 'Likes';
                btn.disabled = false;
            })
            .catch(() => { btn.disabled = false; });
    }

    // ── Delete Post ────────────────────────────────────────────────────
    function deletePost(postId) {
        if (!confirm('Delete this post? This cannot be undone.')) return;
        const fd = new FormData();
        fd.append('postId', postId);
        fd.append('__RequestVerificationToken', antiforgeryToken);

        fetch('/Community/DeletePost', { method: 'POST', body: fd })
            .then(r => r.json())
            .then(data => {
                if (data.success) {
                    document.getElementById('post-' + postId)?.remove();
                    totalPostCount = Math.max(0, totalPostCount - 1);
                    if (totalPostsEl) totalPostsEl.textContent = totalPostCount;
                    if (feed.children.length === 0) feed.innerHTML = emptyStateHtml();
                    showToast('Post deleted.', 'info');
                } else {
                    showToast(data.message || 'Could not delete post.', 'error');
                }
            })
            .catch(() => showToast('Error deleting post.', 'error'));
    }

    // ── Comments ───────────────────────────────────────────────────────
    function toggleComments(postId) {
        const section = document.getElementById('comments-' + postId);
        if (!section) return;
        const isOpen = section.classList.contains('open');
        if (isOpen) {
            section.classList.remove('open');
        } else {
            section.classList.add('open');
            loadComments(postId);
        }
    }

    function loadComments(postId) {
        const listEl = document.getElementById('comments-list-' + postId);
        if (!listEl) return;
        listEl.innerHTML = '<div style="font-size:.82rem;color:#94a3b8;padding:8px 0;">Loading…</div>';

        fetch('/Community/GetComments?postId=' + postId, {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .then(r => r.json())
            .then(data => {
                if (!data.success) { listEl.innerHTML = ''; return; }
                renderComments(postId, data.comments);
            })
            .catch(() => { listEl.innerHTML = ''; });
    }

    function renderComments(postId, comments) {
        const listEl = document.getElementById('comments-list-' + postId);
        if (!listEl) return;
        if (!comments || comments.length === 0) {
            listEl.innerHTML = '<div style="font-size:.82rem;color:#94a3b8;padding:8px 0;">No comments yet. Be the first!</div>';
            return;
        }
        listEl.innerHTML = comments.map(c => commentItemHtml(c)).join('');
        listEl.querySelectorAll('.cm-comment-delete').forEach(btn => {
            btn.addEventListener('click', () => deleteComment(btn.dataset.commentId, postId));
        });
    }

    function commentItemHtml(c) {
        const avatarUrl = `https://ui-avatars.com/api/?name=${encodeURIComponent(c.authorName || 'U')}&background=1baebe&color=fff&size=60`;
        const deleteBtn = c.isMyComment
            ? `<button class="cm-comment-delete" data-comment-id="${c.communityCommentId}" title="Delete"><i class="fas fa-times"></i></button>`
            : '';
        return `
<div class="cm-comment-item" id="comment-${c.communityCommentId}">
    <img class="cm-comment-avatar" src="${avatarUrl}" alt="${escHtml(c.authorName)}" />
    <div class="cm-comment-bubble">
        <div class="cm-comment-author">${escHtml(c.authorName)} ${deleteBtn}</div>
        <div class="cm-comment-text">${escHtml(c.content)}</div>
        <div class="cm-comment-time">${formatTimeAgo(c.createdAt)}</div>
    </div>
</div>`;
    }

    function submitComment(postId, textarea) {
        const content = textarea.value.trim();
        if (!content) return;
        const sendBtn = document.querySelector(`.cm-comment-send-btn[data-post-id="${postId}"]`);
        if (sendBtn) sendBtn.disabled = true;

        const fd = new FormData();
        fd.append('postId', postId);
        fd.append('content', content);
        fd.append('__RequestVerificationToken', antiforgeryToken);

        fetch('/Community/AddComment', { method: 'POST', body: fd })
            .then(r => r.json())
            .then(data => {
                if (data.success) {
                    textarea.value = '';
                    const listEl = document.getElementById('comments-list-' + postId);
                    if (listEl) {
                        // Remove the "no comments" placeholder if present
                        if (listEl.querySelector('div[style]')) listEl.innerHTML = '';
                        const div = document.createElement('div');
                        div.innerHTML = commentItemHtml(data.comment);
                        listEl.appendChild(div.firstElementChild);
                        div.firstElementChild && listEl.querySelectorAll('.cm-comment-delete').forEach(btn => {
                            btn.addEventListener('click', () => deleteComment(btn.dataset.commentId, postId));
                        });
                    }
                    // Update comment count badge
                    const countEl = document.getElementById('cc-' + postId);
                    if (countEl) {
                        const newCount = parseInt(countEl.textContent || '0') + 1;
                        countEl.textContent = newCount;
                    }
                } else {
                    showToast(data.message || 'Could not post comment.', 'error');
                }
                if (sendBtn) sendBtn.disabled = false;
            })
            .catch(() => {
                showToast('Error posting comment.', 'error');
                if (sendBtn) sendBtn.disabled = false;
            });
    }

    function deleteComment(commentId, postId) {
        const fd = new FormData();
        fd.append('commentId', commentId);
        fd.append('__RequestVerificationToken', antiforgeryToken);

        fetch('/Community/DeleteComment', { method: 'POST', body: fd })
            .then(r => r.json())
            .then(data => {
                if (data.success) {
                    document.getElementById('comment-' + commentId)?.remove();
                    const countEl = document.getElementById('cc-' + postId);
                    if (countEl) {
                        const n = Math.max(0, parseInt(countEl.textContent || '0') - 1);
                        countEl.textContent = n;
                    }
                } else {
                    showToast(data.message || 'Could not delete comment.', 'error');
                }
            })
            .catch(() => showToast('Error deleting comment.', 'error'));
    }

    // ── Create Post ────────────────────────────────────────────────────
    function handleCreatePost(e) {
        e.preventDefault();
        const titleEl = document.getElementById('cmPostTitle');
        const contentEl = document.getElementById('cmPostContent');
        const categoryEl = document.getElementById('cmPostCategory');
        const submitBtn = document.getElementById('cmSubmitPost');

        const title = titleEl?.value.trim();
        const content = contentEl?.value.trim();
        const category = categoryEl?.value || 'General';

        if (!title || !content) {
            showToast('Please fill in title and content.', 'error');
            return;
        }

        if (submitBtn) { submitBtn.disabled = true; submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Posting…'; }

        const fd = new FormData();
        fd.append('title', title);
        fd.append('content', content);
        fd.append('category', category);
        const imageFile = imageInput?.files?.[0];
        if (imageFile) fd.append('image', imageFile);
        fd.append('__RequestVerificationToken', antiforgeryToken);

        fetch('/Community/CreatePost', { method: 'POST', body: fd })
            .then(r => r.json())
            .then(data => {
                if (data.success) {
                    closeModal();
                    // Reload first page to show the new post
                    currentPage = 1;
                    currentCategory = 'All';
                    currentSearch = '';
                    categoryBtns.forEach(b => b.classList.toggle('active', b.dataset.category === 'All'));
                    if (searchInput) searchInput.value = '';
                    loadPosts();
                    showToast('Post shared with the community!', 'success');
                } else {
                    showToast(data.message || 'Could not create post.', 'error');
                }
            })
            .catch(() => showToast('Error creating post.', 'error'))
            .finally(() => {
                if (submitBtn) { submitBtn.disabled = false; submitBtn.innerHTML = '<i class="fas fa-paper-plane"></i> Post'; }
            });
    }

    // ── Pagination ─────────────────────────────────────────────────────
    function renderPagination() {
        if (!paginationWrap) return;
        if (totalPages <= 1) { paginationWrap.innerHTML = ''; return; }

        let html = `<button class="cm-page-btn" id="cmPrevPage" ${currentPage === 1 ? 'disabled' : ''}>
            <i class="fas fa-chevron-left"></i>
        </button>`;

        for (let i = 1; i <= totalPages; i++) {
            if (totalPages > 7 && i > 2 && i < totalPages - 1 && Math.abs(i - currentPage) > 1) {
                if (i === 3 || i === totalPages - 2) html += `<span style="padding:0 4px;color:#94a3b8">…</span>`;
                continue;
            }
            html += `<button class="cm-page-btn ${i === currentPage ? 'active' : ''}" data-page="${i}">${i}</button>`;
        }

        html += `<button class="cm-page-btn" id="cmNextPage" ${currentPage === totalPages ? 'disabled' : ''}>
            <i class="fas fa-chevron-right"></i>
        </button>`;

        paginationWrap.innerHTML = html;

        paginationWrap.querySelectorAll('.cm-page-btn[data-page]').forEach(btn => {
            btn.addEventListener('click', () => {
                currentPage = parseInt(btn.dataset.page);
                loadPosts();
                window.scrollTo({ top: 0, behavior: 'smooth' });
            });
        });

        document.getElementById('cmPrevPage')?.addEventListener('click', () => {
            if (currentPage > 1) { currentPage--; loadPosts(); }
        });
        document.getElementById('cmNextPage')?.addEventListener('click', () => {
            if (currentPage < totalPages) { currentPage++; loadPosts(); }
        });
    }

    // ── Skeleton Loader ────────────────────────────────────────────────
    function showSkeletons() {
        if (!feed) return;
        feed.innerHTML = [1, 2, 3].map(() => `
<div class="cm-skeleton-card">
    <div class="cm-skel-row">
        <div class="cm-skel" style="width:40px;height:40px;border-radius:50%;flex-shrink:0"></div>
        <div style="flex:1;display:flex;flex-direction:column;gap:6px">
            <div class="cm-skel" style="height:12px;width:40%"></div>
            <div class="cm-skel" style="height:10px;width:25%"></div>
        </div>
    </div>
    <div class="cm-skel" style="height:14px;width:70%;margin-top:4px"></div>
    <div class="cm-skel" style="height:10px;width:100%"></div>
    <div class="cm-skel" style="height:10px;width:85%"></div>
</div>`).join('');
    }

    function showError() {
        if (!feed) return;
        feed.innerHTML = `<div class="cm-empty-state">
            <div class="cm-empty-icon" style="background:#fef2f2;color:#ef4444"><i class="fas fa-exclamation-circle"></i></div>
            <h4 class="cm-empty-title">Could not load posts</h4>
            <p class="cm-empty-sub">Please refresh the page to try again.</p>
        </div>`;
    }

    // ── Toast ──────────────────────────────────────────────────────────
    function showToast(msg, type = 'info') {
        let toast = document.getElementById('cmToast');
        if (!toast) {
            toast = document.createElement('div');
            toast.id = 'cmToast';
            toast.className = 'cm-toast';
            document.body.appendChild(toast);
        }
        toast.className = `cm-toast ${type}`;
        const icons = { success: 'fas fa-check-circle', error: 'fas fa-times-circle', info: 'fas fa-info-circle' };
        toast.innerHTML = `<i class="${icons[type] || icons.info}"></i> ${escHtml(msg)}`;
        toast.classList.add('show');
        setTimeout(() => toast.classList.remove('show'), 3000);
    }

    // ── Helpers ────────────────────────────────────────────────────────
    function escHtml(str) {
        return String(str ?? '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function formatTimeAgo(dateStr) {
        const d = new Date(dateStr);
        const now = new Date();
        const diff = Math.floor((now - d) / 1000);
        if (diff < 60) return 'Just now';
        if (diff < 3600) return Math.floor(diff / 60) + 'm ago';
        if (diff < 86400) return Math.floor(diff / 3600) + 'h ago';
        if (diff < 2592000) return Math.floor(diff / 86400) + 'd ago';
        return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
    }

    // ── Boot ───────────────────────────────────────────────────────────
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
