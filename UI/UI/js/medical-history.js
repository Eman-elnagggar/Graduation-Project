// ================================
// Medical History Page JavaScript
// ================================

document.addEventListener('DOMContentLoaded', function() {
    
    // Filter Elements
    const searchInput = document.getElementById('historySearch');
    const eventTypeFilter = document.getElementById('eventTypeFilter');
    const dateRangeFilter = document.getElementById('dateRangeFilter');
    const statusFilter = document.getElementById('statusFilter');
    const clearFiltersBtn = document.getElementById('clearFilters');
    const exportBtn = document.getElementById('exportHistory');
    const customDateRange = document.getElementById('customDateRange');
    const loadMoreBtn = document.getElementById('loadMoreHistory');

    // Timeline Items
    const timelineItems = document.querySelectorAll('.timeline-item');

    // Search functionality
    if (searchInput) {
        searchInput.addEventListener('input', debounce(function() {
            filterTimeline();
        }, 300));
    }

    // Event Type Filter
    if (eventTypeFilter) {
        eventTypeFilter.addEventListener('change', filterTimeline);
    }

    // Date Range Filter
    if (dateRangeFilter) {
        dateRangeFilter.addEventListener('change', function() {
            if (this.value === 'custom') {
                customDateRange.style.display = 'block';
            } else {
                customDateRange.style.display = 'none';
                filterTimeline();
            }
        });
    }

    // Status Filter
    if (statusFilter) {
        statusFilter.addEventListener('change', filterTimeline);
    }

    // Clear Filters
    if (clearFiltersBtn) {
        clearFiltersBtn.addEventListener('click', function() {
            if (searchInput) searchInput.value = '';
            if (eventTypeFilter) eventTypeFilter.value = 'all';
            if (dateRangeFilter) dateRangeFilter.value = 'month';
            if (statusFilter) statusFilter.value = 'all';
            if (customDateRange) customDateRange.style.display = 'none';
            
            filterTimeline();
            showNotification('Filters cleared', 'success');
        });
    }

    // Export History
    if (exportBtn) {
        exportBtn.addEventListener('click', function() {
            showExportModal();
        });
    }

    // Load More
    if (loadMoreBtn) {
        loadMoreBtn.addEventListener('click', function() {
            this.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Loading...';
            
            setTimeout(() => {
                this.innerHTML = '<i class="fas fa-history"></i> Load Earlier Records';
                showNotification('All records have been loaded', 'info');
            }, 1500);
        });
    }

    // Filter Timeline Function
    function filterTimeline() {
        const searchTerm = searchInput?.value.toLowerCase() || '';
        const eventType = eventTypeFilter?.value || 'all';
        const status = statusFilter?.value || 'all';

        let visibleCount = 0;

        timelineItems.forEach(item => {
            const itemType = item.dataset.type || '';
            const itemStatus = item.dataset.status || '';
            const itemText = item.textContent.toLowerCase();

            let showItem = true;

            // Search filter
            if (searchTerm && !itemText.includes(searchTerm)) {
                showItem = false;
            }

            // Event type filter
            if (eventType !== 'all' && itemType !== eventType) {
                showItem = false;
            }

            // Status filter
            if (status !== 'all' && itemStatus !== status) {
                showItem = false;
            }

            // Show/hide item with animation
            if (showItem) {
                item.style.display = 'flex';
                item.style.animation = 'fadeIn 0.3s ease';
                visibleCount++;
            } else {
                item.style.display = 'none';
            }
        });

        // Show/hide date groups based on visible items
        document.querySelectorAll('.timeline-date-group').forEach(group => {
            const visibleItems = group.querySelectorAll('.timeline-item[style*="display: flex"], .timeline-item:not([style*="display"])');
            const hasVisible = Array.from(group.querySelectorAll('.timeline-item')).some(item => {
                return item.style.display !== 'none';
            });
            
            group.style.display = hasVisible ? 'block' : 'none';
        });

        // Update results count
        updateResultsCount(visibleCount);
    }

    // Update Results Count
    function updateResultsCount(count) {
        let countEl = document.querySelector('.results-count');
        
        if (!countEl) {
            countEl = document.createElement('div');
            countEl.className = 'results-count';
            const filtersCard = document.querySelector('.history-filters .card');
            if (filtersCard) {
                filtersCard.appendChild(countEl);
            }
        }
        
        countEl.textContent = `${count} record${count !== 1 ? 's' : ''} found`;
    }

    // Debounce Function
    function debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    // Export Modal
    function showExportModal() {
        const modal = document.createElement('div');
        modal.className = 'export-modal';
        modal.innerHTML = `
            <div class="modal-overlay"></div>
            <div class="modal-content">
                <div class="modal-header">
                    <h3><i class="fas fa-download"></i> Export Medical History</h3>
                    <button class="modal-close"><i class="fas fa-times"></i></button>
                </div>
                <div class="modal-body">
                    <p>Choose the format to export your medical history:</p>
                    
                    <div class="export-options">
                        <label class="export-option">
                            <input type="radio" name="exportFormat" value="pdf" checked>
                            <div class="option-content">
                                <i class="fas fa-file-pdf"></i>
                                <span>PDF Document</span>
                                <small>Best for printing and sharing</small>
                            </div>
                        </label>
                        <label class="export-option">
                            <input type="radio" name="exportFormat" value="excel">
                            <div class="option-content">
                                <i class="fas fa-file-excel"></i>
                                <span>Excel Spreadsheet</span>
                                <small>Best for data analysis</small>
                            </div>
                        </label>
                        <label class="export-option">
                            <input type="radio" name="exportFormat" value="json">
                            <div class="option-content">
                                <i class="fas fa-file-code"></i>
                                <span>JSON Data</span>
                                <small>For technical/backup purposes</small>
                            </div>
                        </label>
                    </div>

                    <div class="export-range">
                        <label>Date Range:</label>
                        <select class="form-select">
                            <option value="all">All Time</option>
                            <option value="year">Last Year</option>
                            <option value="6months">Last 6 Months</option>
                            <option value="3months">Last 3 Months</option>
                            <option value="month">Last Month</option>
                        </select>
                    </div>
                </div>
                <div class="modal-footer">
                    <button class="btn btn-secondary modal-cancel">Cancel</button>
                    <button class="btn btn-primary modal-export">
                        <i class="fas fa-download"></i> Export
                    </button>
                </div>
            </div>
        `;

        document.body.appendChild(modal);

        // Close modal handlers
        modal.querySelector('.modal-close').addEventListener('click', () => modal.remove());
        modal.querySelector('.modal-cancel').addEventListener('click', () => modal.remove());
        modal.querySelector('.modal-overlay').addEventListener('click', () => modal.remove());

        // Export handler
        modal.querySelector('.modal-export').addEventListener('click', function() {
            this.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Exporting...';
            
            setTimeout(() => {
                modal.remove();
                showNotification('Medical history exported successfully!', 'success');
            }, 1500);
        });
    }

    // View Details Modal
    document.querySelectorAll('.timeline-content').forEach(content => {
        const viewBtn = content.querySelector('.btn-ghost');
        if (viewBtn && viewBtn.textContent.includes('View')) {
            viewBtn.addEventListener('click', function(e) {
                e.preventDefault();
                showDetailsModal(content);
            });
        }
    });

    function showDetailsModal(content) {
        const title = content.querySelector('h4')?.textContent || 'Details';
        const body = content.querySelector('.timeline-body')?.innerHTML || '';

        const modal = document.createElement('div');
        modal.className = 'details-modal';
        modal.innerHTML = `
            <div class="modal-overlay"></div>
            <div class="modal-content large">
                <div class="modal-header">
                    <h3>${title}</h3>
                    <button class="modal-close"><i class="fas fa-times"></i></button>
                </div>
                <div class="modal-body">
                    ${body}
                </div>
                <div class="modal-footer">
                    <button class="btn btn-secondary modal-close-btn">Close</button>
                    <button class="btn btn-outline">
                        <i class="fas fa-print"></i> Print
                    </button>
                    <button class="btn btn-outline">
                        <i class="fas fa-share"></i> Share
                    </button>
                </div>
            </div>
        `;

        document.body.appendChild(modal);

        // Close handlers
        modal.querySelector('.modal-close').addEventListener('click', () => modal.remove());
        modal.querySelector('.modal-close-btn').addEventListener('click', () => modal.remove());
        modal.querySelector('.modal-overlay').addEventListener('click', () => modal.remove());
    }

    // Notification Function
    function showNotification(message, type) {
        const existing = document.querySelector('.history-notification');
        if (existing) existing.remove();

        const notification = document.createElement('div');
        notification.className = `history-notification ${type}`;
        notification.innerHTML = `
            <i class="fas ${type === 'success' ? 'fa-check-circle' : type === 'error' ? 'fa-exclamation-circle' : 'fa-info-circle'}"></i>
            <span>${message}</span>
        `;

        document.body.appendChild(notification);

        setTimeout(() => {
            notification.style.animation = 'slideOut 0.3s ease forwards';
            setTimeout(() => notification.remove(), 300);
        }, 3000);
    }

    // Add notification and modal styles
    const styles = document.createElement('style');
    styles.textContent = `
        .history-notification {
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
            animation: slideIn 0.3s ease;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        }
        .history-notification.success {
            background: #E8F5E9;
            color: #2E7D32;
        }
        .history-notification.error {
            background: #FFEBEE;
            color: #C62828;
        }
        .history-notification.info {
            background: #E3F2FD;
            color: #1565C0;
        }
        .results-count {
            margin-top: 12px;
            font-size: 0.9rem;
            color: var(--text-muted);
        }
        .export-modal,
        .details-modal {
            position: fixed;
            inset: 0;
            z-index: 9999;
            display: flex;
            align-items: center;
            justify-content: center;
        }
        .modal-overlay {
            position: absolute;
            inset: 0;
            background: rgba(0,0,0,0.5);
        }
        .modal-content {
            position: relative;
            background: white;
            border-radius: 16px;
            max-width: 500px;
            width: 90%;
            animation: modalSlideIn 0.3s ease;
            overflow: hidden;
        }
        .modal-content.large {
            max-width: 700px;
            max-height: 80vh;
            overflow-y: auto;
        }
        .modal-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 20px 24px;
            border-bottom: 1px solid #E2E8F0;
        }
        .modal-header h3 {
            display: flex;
            align-items: center;
            gap: 10px;
            font-size: 1.1rem;
            margin: 0;
        }
        .modal-close {
            background: none;
            border: none;
            font-size: 1.2rem;
            cursor: pointer;
            color: #64748B;
            padding: 4px;
        }
        .modal-body {
            padding: 24px;
        }
        .modal-footer {
            display: flex;
            gap: 12px;
            justify-content: flex-end;
            padding: 16px 24px;
            border-top: 1px solid #E2E8F0;
            background: #F8FAFC;
        }
        .export-options {
            display: flex;
            flex-direction: column;
            gap: 12px;
            margin: 20px 0;
        }
        .export-option {
            cursor: pointer;
        }
        .export-option input {
            display: none;
        }
        .export-option .option-content {
            display: flex;
            align-items: center;
            gap: 14px;
            padding: 16px;
            border: 2px solid #E2E8F0;
            border-radius: 10px;
            transition: all 0.2s;
        }
        .export-option input:checked + .option-content {
            border-color: #1BAEBE;
            background: rgba(27, 174, 190, 0.05);
        }
        .export-option .option-content i {
            font-size: 1.5rem;
            color: #1BAEBE;
        }
        .export-option .option-content span {
            font-weight: 500;
        }
        .export-option .option-content small {
            display: block;
            font-size: 0.8rem;
            color: #64748B;
        }
        .export-range {
            margin-top: 20px;
        }
        .export-range label {
            display: block;
            margin-bottom: 8px;
            font-weight: 500;
        }
        @keyframes slideIn {
            from { opacity: 0; transform: translateX(100px); }
            to { opacity: 1; transform: translateX(0); }
        }
        @keyframes slideOut {
            from { opacity: 1; transform: translateX(0); }
            to { opacity: 0; transform: translateX(100px); }
        }
        @keyframes modalSlideIn {
            from { opacity: 0; transform: translateY(-20px); }
            to { opacity: 1; transform: translateY(0); }
        }
    `;
    document.head.appendChild(styles);

    console.log('Medical History page initialized! 📋');
});