// ================================
// Alerts Page JavaScript
// ================================

document.addEventListener('DOMContentLoaded', function() {
    
    // Filter Tabs
    const filterTabs = document.querySelectorAll('.filter-tab');
    const alertCards = document.querySelectorAll('.alert-card');
    const alertGroups = document.querySelectorAll('.alert-group');

    filterTabs.forEach(tab => {
        tab.addEventListener('click', function() {
            // Remove active from all tabs
            filterTabs.forEach(t => t.classList.remove('active'));
            // Add active to clicked tab
            this.classList.add('active');

            const filter = this.dataset.filter;
            filterAlerts(filter);
        });
    });

    function filterAlerts(filter) {
        let visibleCount = 0;

        alertCards.forEach(card => {
            const type = card.dataset.type;
            const status = card.dataset.status;
            let show = false;

            switch(filter) {
                case 'all':
                    show = true;
                    break;
                case 'unread':
                    show = status === 'unread';
                    break;
                case 'health':
                    show = type === 'health';
                    break;
                case 'doctor':
                    show = type === 'doctor';
                    break;
                case 'appointment':
                    show = type === 'appointment';
                    break;
                case 'system':
                    show = type === 'system';
                    break;
                default:
                    show = true;
            }

            if (show) {
                card.style.display = 'flex';
                card.style.animation = 'fadeIn 0.3s ease';
                visibleCount++;
            } else {
                card.style.display = 'none';
            }
        });

        // Show/hide groups based on visible cards
        alertGroups.forEach(group => {
            const visibleCards = group.querySelectorAll('.alert-card[style*="display: flex"]');
            if (visibleCards.length === 0) {
                group.style.display = 'none';
            } else {
                group.style.display = 'block';
            }
        });

        // Update count
        updateResultsInfo(visibleCount);
    }

    function updateResultsInfo(count) {
        // Optional: Show results count
        console.log(`Showing ${count} alerts`);
    }

    // Mark All Read
    const markAllReadBtn = document.getElementById('markAllRead');
    if (markAllReadBtn) {
        markAllReadBtn.addEventListener('click', function() {
            const unreadCards = document.querySelectorAll('.alert-card.unread');
            unreadCards.forEach(card => {
                card.classList.remove('unread');
            });
            
            // Update unread count
            updateUnreadCount();
            showNotification('All alerts marked as read', 'success');
        });
    }

    // Clear Resolved
    const clearResolvedBtn = document.getElementById('clearResolved');
    if (clearResolvedBtn) {
        clearResolvedBtn.addEventListener('click', function() {
            if (confirm('Are you sure you want to clear all resolved alerts?')) {
                const resolvedCards = document.querySelectorAll('.alert-card[data-status="resolved"]');
                resolvedCards.forEach(card => {
                    card.style.animation = 'fadeOut 0.3s ease';
                    setTimeout(() => card.remove(), 300);
                });
                showNotification('Resolved alerts cleared', 'success');
            }
        });
    }

    // Individual Mark as Read
    document.querySelectorAll('.mark-read').forEach(btn => {
        btn.addEventListener('click', function() {
            const card = this.closest('.alert-card');
            card.classList.remove('unread');
            this.innerHTML = '<i class="fas fa-check-double"></i> Read';
            this.disabled = true;
            updateUnreadCount();
        });
    });

    // Dismiss Alert
    document.querySelectorAll('.alert-dismiss').forEach(btn => {
        btn.addEventListener('click', function() {
            const card = this.closest('.alert-card');
            card.style.animation = 'slideOutRight 0.3s ease';
            setTimeout(() => {
                card.remove();
                updateGroupCounts();
            }, 300);
        });
    });

    // Collapsible Groups
    const resolvedHeader = document.getElementById('resolvedHeader');
    const resolvedContent = document.getElementById('resolvedContent');
    
    if (resolvedHeader && resolvedContent) {
        resolvedHeader.addEventListener('click', function() {
            const group = this.closest('.alert-group');
            group.classList.toggle('collapsed');
            
            if (resolvedContent.style.display === 'none') {
                resolvedContent.style.display = 'block';
            } else {
                resolvedContent.style.display = 'none';
            }
        });
    }

    // Doctor Approval Actions
    document.querySelectorAll('.alert-card[data-type="doctor"] .btn-success').forEach(btn => {
        btn.addEventListener('click', function() {
            const card = this.closest('.alert-card');
            
            // Show confirmation
            if (confirm('Approve this doctor to access your medical records?')) {
                // Change card to approved state
                card.classList.remove('unread', 'info');
                card.classList.add('success');
                card.querySelector('.alert-header h4').textContent = 'Doctor Approved';
                card.querySelector('.alert-actions').innerHTML = `
                    <button class="btn btn-ghost btn-sm">
                        <i class="fas fa-calendar-plus"></i> Book Appointment
                    </button>
                    <button class="btn btn-ghost btn-sm">
                        <i class="fas fa-comment"></i> Send Message
                    </button>
                `;
                
                showNotification('Doctor request approved successfully!', 'success');
                updateUnreadCount();
            }
        });
    });

    document.querySelectorAll('.alert-card[data-type="doctor"] .btn-danger').forEach(btn => {
        btn.addEventListener('click', function() {
            const card = this.closest('.alert-card');
            
            if (confirm('Decline this doctor request?')) {
                card.style.animation = 'fadeOut 0.3s ease';
                setTimeout(() => card.remove(), 300);
                showNotification('Doctor request declined', 'info');
            }
        });
    });

    // Appointment Confirmation
    document.querySelectorAll('.alert-card[data-type="appointment"] .btn-primary').forEach(btn => {
        if (btn.textContent.includes('Confirm')) {
            btn.addEventListener('click', function() {
                const card = this.closest('.alert-card');
                card.classList.remove('unread', 'warning');
                card.classList.add('success');
                
                this.innerHTML = '<i class="fas fa-check-double"></i> Confirmed';
                this.disabled = true;
                this.classList.remove('btn-primary');
                this.classList.add('btn-ghost');
                
                showNotification('Appointment confirmed!', 'success');
                updateUnreadCount();
            });
        }
    });

    // Load More
    const loadMoreBtn = document.getElementById('loadMoreAlerts');
    if (loadMoreBtn) {
        loadMoreBtn.addEventListener('click', function() {
            this.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Loading...';
            
            setTimeout(() => {
                this.innerHTML = '<i class="fas fa-ellipsis-h"></i> Load More Alerts';
                showNotification('All alerts have been loaded', 'info');
            }, 1500);
        });
    }

    // Update Unread Count
    function updateUnreadCount() {
        const unreadCount = document.querySelectorAll('.alert-card.unread').length;
        
        // Update badge in header
        const badge = document.querySelector('.notification .badge');
        if (badge) {
            badge.textContent = unreadCount;
            if (unreadCount === 0) {
                badge.style.display = 'none';
            } else {
                badge.style.display = 'flex';
            }
        }

        // Update filter tab count
        const unreadTab = document.querySelector('.filter-tab[data-filter="unread"] .count');
        if (unreadTab) {
            unreadTab.textContent = unreadCount;
        }

        // Update stat card
        const unreadStat = document.querySelector('.alert-stat-card.unread .stat-number');
        if (unreadStat) {
            unreadStat.textContent = unreadCount;
        }
    }

    // Update Group Counts
    function updateGroupCounts() {
        alertGroups.forEach(group => {
            const cards = group.querySelectorAll('.alert-card');
            const countEl = group.querySelector('.group-count');
            if (countEl) {
                countEl.textContent = `${cards.length} alert${cards.length !== 1 ? 's' : ''}`;
            }
        });
    }

    // Notification Function
    function showNotification(message, type) {
        const existing = document.querySelector('.alert-notification');
        if (existing) existing.remove();

        const notification = document.createElement('div');
        notification.className = `alert-notification ${type}`;
        
        let icon = 'fa-info-circle';
        if (type === 'success') icon = 'fa-check-circle';
        if (type === 'error') icon = 'fa-exclamation-circle';
        if (type === 'warning') icon = 'fa-exclamation-triangle';

        notification.innerHTML = `
            <i class="fas ${icon}"></i>
            <span>${message}</span>
        `;

        document.body.appendChild(notification);

        setTimeout(() => {
            notification.style.animation = 'slideOutRight 0.3s ease forwards';
            setTimeout(() => notification.remove(), 300);
        }, 3000);
    }

    // Add notification styles
    const styles = document.createElement('style');
    styles.textContent = `
        .alert-notification {
            position: fixed;
            top: 24px;
            right: 24px;
            padding: 14px 20px;
            border-radius: 10px;
            display: flex;
            align-items: center;
            gap: 10px;
            font-weight: 500;
            z-index: 9999;
            animation: slideInRight 0.3s ease;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        }
        .alert-notification.success {
            background: #E8F5E9;
            color: #2E7D32;
        }
        .alert-notification.error {
            background: #FFEBEE;
            color: #C62828;
        }
        .alert-notification.warning {
            background: #FFF3E0;
            color: #E65100;
        }
        .alert-notification.info {
            background: #E3F2FD;
            color: #1565C0;
        }
        @keyframes slideInRight {
            from {
                opacity: 0;
                transform: translateX(100px);
            }
            to {
                opacity: 1;
                transform: translateX(0);
            }
        }
        @keyframes slideOutRight {
            from {
                opacity: 1;
                transform: translateX(0);
            }
            to {
                opacity: 0;
                transform: translateX(100px);
            }
        }
        @keyframes fadeOut {
            from { opacity: 1; }
            to { opacity: 0; }
        }
    `;
    document.head.appendChild(styles);

    console.log('Alerts page initialized! 🔔');
});