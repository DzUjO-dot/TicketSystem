// ============================================
// IT SERVICE HELPDESK - JAVASCRIPT
// ============================================

(function() {
    'use strict';

    // ============================================
    // Sidebar Toggle (Mobile)
    // ============================================
    window.toggleSidebar = function() {
        const sidebar = document.getElementById('sidebar');
        if (sidebar) {
            sidebar.classList.toggle('show');
        }
    };

    // Close sidebar when clicking outside on mobile
    document.addEventListener('click', function(e) {
        const sidebar = document.getElementById('sidebar');
        const sidebarToggle = document.querySelector('.sidebar-toggle');
        
        if (sidebar && sidebar.classList.contains('show')) {
            if (!sidebar.contains(e.target) && !sidebarToggle?.contains(e.target)) {
                sidebar.classList.remove('show');
            }
        }
    });

    // ============================================
    // Auto-dismiss Alerts
    // ============================================
    document.addEventListener('DOMContentLoaded', function() {
        const alerts = document.querySelectorAll('.alert-dismissible');
        alerts.forEach(function(alert) {
            setTimeout(function() {
                const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
                if (bsAlert) {
                    bsAlert.close();
                }
            }, 5000); // Auto-dismiss after 5 seconds
        });
    });

    // ============================================
    // Form Validation Styling
    // ============================================
    document.addEventListener('DOMContentLoaded', function() {
        const forms = document.querySelectorAll('form');
        forms.forEach(function(form) {
            form.addEventListener('submit', function(e) {
                if (!form.checkValidity()) {
                    e.preventDefault();
                    e.stopPropagation();
                }
                form.classList.add('was-validated');
            });
        });
    });

    // ============================================
    // File Input Preview
    // ============================================
    document.addEventListener('DOMContentLoaded', function() {
        const fileInputs = document.querySelectorAll('input[type="file"]');
        fileInputs.forEach(function(input) {
            input.addEventListener('change', function() {
                const files = this.files;
                const fileList = document.createElement('div');
                fileList.className = 'mt-2 small text-muted';
                
                if (files.length > 0) {
                    let html = '<strong>Wybrane pliki:</strong><ul class="mb-0">';
                    for (let i = 0; i < files.length; i++) {
                        const size = (files[i].size / 1024).toFixed(1);
                        html += `<li>${files[i].name} (${size} KB)</li>`;
                    }
                    html += '</ul>';
                    fileList.innerHTML = html;
                    
                    // Remove previous preview
                    const existingPreview = this.parentElement.querySelector('.file-preview');
                    if (existingPreview) {
                        existingPreview.remove();
                    }
                    
                    fileList.className += ' file-preview';
                    this.parentElement.appendChild(fileList);
                }
            });
        });
    });

    // ============================================
    // Confirm Delete Actions
    // ============================================
    document.addEventListener('DOMContentLoaded', function() {
        const deleteButtons = document.querySelectorAll('[data-confirm-delete]');
        deleteButtons.forEach(function(button) {
            button.addEventListener('click', function(e) {
                if (!confirm('Czy na pewno chcesz usunąć ten element?')) {
                    e.preventDefault();
                }
            });
        });
    });

    // ============================================
    // Tooltip Initialization
    // ============================================
    document.addEventListener('DOMContentLoaded', function() {
        const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
        tooltipTriggerList.forEach(function(tooltipTriggerEl) {
            new bootstrap.Tooltip(tooltipTriggerEl);
        });
    });

    // ============================================
    // Dropdowns inside table-responsive
    // Popper domyślnie klipuje dropdown do kontenera z overflow:auto.
    // Strategia 'fixed' pozycjonuje względem viewportu i omija to ograniczenie.
    // ============================================
    document.addEventListener('DOMContentLoaded', function() {
        document.querySelectorAll('.table-responsive [data-bs-toggle="dropdown"]').forEach(function(el) {
            new bootstrap.Dropdown(el, {
                popperConfig: { strategy: 'fixed' }
            });
        });
    });

    // ============================================
    // Search Input Enhancement
    // ============================================
    document.addEventListener('DOMContentLoaded', function() {
        const searchInputs = document.querySelectorAll('.navbar-search input');
        searchInputs.forEach(function(input) {
            input.addEventListener('keypress', function(e) {
                if (e.key === 'Enter') {
                    this.closest('form').submit();
                }
            });
        });
    });

    // ============================================
    // Priority Color Helper
    // ============================================
    window.getPriorityColor = function(priority) {
        const colors = {
            'Low': '#10b981',
            'Medium': '#f59e0b',
            'High': '#f97316',
            'Critical': '#ef4444'
        };
        return colors[priority] || '#64748b';
    };

    // ============================================
    // Status Color Helper
    // ============================================
    window.getStatusColor = function(status) {
        const colors = {
            'New': '#14b8a6',
            'Open': '#7c3aed',
            'InProgress': '#6366f1',
            'WaitingForUser': '#ca8a04',
            'Resolved': '#10b981',
            'Closed': '#64748b',
            'Rejected': '#ef4444'
        };
        return colors[status] || '#64748b';
    };

    // ============================================
    // Loading State Helper
    // ============================================
    window.showLoading = function(element) {
        if (element) {
            element.disabled = true;
            element.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Ładowanie...';
        }
    };

    window.hideLoading = function(element, originalText) {
        if (element) {
            element.disabled = false;
            element.innerHTML = originalText;
        }
    };

    // ============================================
    // Copy to Clipboard
    // ============================================
    window.copyToClipboard = function(text) {
        navigator.clipboard.writeText(text).then(function() {
            // Show toast or notification
            showToast('Skopiowano do schowka!');
        }).catch(function(err) {
            console.error('Błąd kopiowania: ', err);
        });
    };

    // ============================================
    // Toast Notification
    // ============================================
    window.showToast = function(message, type = 'success') {
        const toastContainer = document.querySelector('.toast-container') || createToastContainer();
        
        const toast = document.createElement('div');
        toast.className = `toast align-items-center text-white bg-${type === 'success' ? 'success' : 'danger'} border-0`;
        toast.setAttribute('role', 'alert');
        toast.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">${message}</div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        `;
        
        toastContainer.appendChild(toast);
        const bsToast = new bootstrap.Toast(toast);
        bsToast.show();
        
        toast.addEventListener('hidden.bs.toast', function() {
            toast.remove();
        });
    };

    function createToastContainer() {
        const container = document.createElement('div');
        container.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        document.body.appendChild(container);
        return container;
    }

    // ============================================
    // Date Formatting
    // ============================================
    window.formatDate = function(dateString) {
        const date = new Date(dateString);
        return date.toLocaleDateString('pl-PL', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric'
        });
    };

    window.formatDateTime = function(dateString) {
        const date = new Date(dateString);
        return date.toLocaleDateString('pl-PL', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    };

    // ============================================
    // Relative Time
    // ============================================
    window.getRelativeTime = function(dateString) {
        const date = new Date(dateString);
        const now = new Date();
        const diff = now - date;
        
        const minutes = Math.floor(diff / 60000);
        const hours = Math.floor(diff / 3600000);
        const days = Math.floor(diff / 86400000);
        
        if (minutes < 1) return 'przed chwilą';
        if (minutes < 60) return `${minutes} min temu`;
        if (hours < 24) return `${hours} godz. temu`;
        if (days < 7) return `${days} dni temu`;
        
        return formatDate(dateString);
    };

    // ============================================
    // Console Greeting
    // ============================================
    console.log('%c IT Service HelpDesk', 'font-size: 24px; font-weight: bold; color: #7c3aed;');
    console.log('%c Projekt studencki - .NET 9 MVC', 'font-size: 12px; color: #64748b;');

})();
