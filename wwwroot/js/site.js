/**
 * FileConvertPro - Main JavaScript File
 * Contains utility functions and event handlers for the application
 */

// Initialize all tooltips and popovers when document is ready
$(document).ready(function () {
    // Initialize tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Initialize popovers
    const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });

    // Auto-dismiss alerts after 5 seconds
    setTimeout(function () {
        $('.alert').alert('close');
    }, 5000);

    // Initialize file upload custom styling
    initFileUpload();

    // Initialize conversion type selection
    initConversionTypeSelection();

    // Initialize auto-refresh for conversion status
    initAutoRefreshStatus();

    // Initialize search functionality
    initSearch();

    // Initialize date range pickers
    initDateRangePickers();
});

/**
 * File Upload Functionality
 */
function initFileUpload() {
    const fileInput = document.getElementById('fileInput');
    const fileUploadZone = document.querySelector('.file-upload-zone');
    const fileNameDisplay = document.getElementById('fileNameDisplay');
    const fileTypeDisplay = document.getElementById('fileTypeDisplay');
    const fileSizeDisplay = document.getElementById('fileSizeDisplay');

    if (!fileInput || !fileUploadZone) return;

    // Handle file selection
    fileInput.addEventListener('change', function (e) {
        if (this.files && this.files.length > 0) {
            const file = this.files[0];
            updateFileInfo(file);
        }
    });

    // Handle drag and drop
    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
        fileUploadZone.addEventListener(eventName, preventDefaults, false);
    });

    function preventDefaults(e) {
        e.preventDefault();
        e.stopPropagation();
    }

    ['dragenter', 'dragover'].forEach(eventName => {
        fileUploadZone.addEventListener(eventName, highlight, false);
    });

    ['dragleave', 'drop'].forEach(eventName => {
        fileUploadZone.addEventListener(eventName, unhighlight, false);
    });

    function highlight() {
        fileUploadZone.classList.add('dragover');
    }

    function unhighlight() {
        fileUploadZone.classList.remove('dragover');
    }

    fileUploadZone.addEventListener('drop', handleDrop, false);

    function handleDrop(e) {
        const dt = e.dataTransfer;
        const files = dt.files;

        if (files && files.length > 0) {
            fileInput.files = files;
            updateFileInfo(files[0]);
        }
    }

    function updateFileInfo(file) {
        if (!fileNameDisplay || !fileTypeDisplay || !fileSizeDisplay) return;

        fileNameDisplay.textContent = file.name;
        fileTypeDisplay.textContent = file.type || 'Unknown type';
        fileSizeDisplay.textContent = formatFileSize(file.size);

        // Show file info section
        const fileInfoSection = document.querySelector('.file-info');
        if (fileInfoSection) {
            fileInfoSection.classList.remove('d-none');
        }

        // Update conversion type options based on file type
        updateConversionOptions(file.type);
    }

    function formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }

    function updateConversionOptions(fileType) {
        const conversionTypeSelect = document.getElementById('conversionType');
        if (!conversionTypeSelect) return;

        // Reset options
        conversionTypeSelect.innerHTML = '<option value="">Select conversion type</option>';

        // Add appropriate options based on file type
        if (fileType.includes('image')) {
            addOption(conversionTypeSelect, 'ImageToJpg', 'Convert to JPG');
            addOption(conversionTypeSelect, 'ImageToPng', 'Convert to PNG');
            addOption(conversionTypeSelect, 'ImageToGif', 'Convert to GIF');
            addOption(conversionTypeSelect, 'ImageToPdf', 'Convert to PDF');
        } else if (fileType.includes('pdf')) {
            addOption(conversionTypeSelect, 'PdfToJpg', 'Convert to JPG');
            addOption(conversionTypeSelect, 'PdfToPng', 'Convert to PNG');
            addOption(conversionTypeSelect, 'PdfToText', 'Convert to Text');
            addOption(conversionTypeSelect, 'PdfToDocx', 'Convert to Word');
        } else if (fileType.includes('word') || fileType.includes('document')) {
            addOption(conversionTypeSelect, 'DocxToPdf', 'Convert to PDF');
            addOption(conversionTypeSelect, 'DocxToText', 'Convert to Text');
            addOption(conversionTypeSelect, 'DocxToHtml', 'Convert to HTML');
        } else if (fileType.includes('text/plain')) {
            addOption(conversionTypeSelect, 'TextToPdf', 'Convert to PDF');
            addOption(conversionTypeSelect, 'TextToDocx', 'Convert to Word');
            addOption(conversionTypeSelect, 'TextToHtml', 'Convert to HTML');
        } else {
            // Generic options for other file types
            addOption(conversionTypeSelect, 'GenericToPdf', 'Convert to PDF');
            addOption(conversionTypeSelect, 'GenericToText', 'Convert to Text');
        }
    }

    function addOption(selectElement, value, text) {
        const option = document.createElement('option');
        option.value = value;
        option.textContent = text;
        selectElement.appendChild(option);
    }
}

/**
 * Conversion Type Selection
 */
function initConversionTypeSelection() {
    const conversionTypeCards = document.querySelectorAll('.conversion-type-card');
    const conversionTypeInput = document.getElementById('selectedConversionType');

    if (!conversionTypeCards.length || !conversionTypeInput) return;

    conversionTypeCards.forEach(card => {
        card.addEventListener('click', function() {
            // Remove selected class from all cards
            conversionTypeCards.forEach(c => c.classList.remove('selected'));
            
            // Add selected class to clicked card
            this.classList.add('selected');
            
            // Update hidden input value
            const conversionType = this.getAttribute('data-conversion-type');
            conversionTypeInput.value = conversionType;
        });
    });
}

/**
 * Auto-refresh for conversion status
 */
function initAutoRefreshStatus() {
    const statusElement = document.getElementById('conversionStatus');
    const conversionId = statusElement?.getAttribute('data-conversion-id');
    const statusRefreshInterval = 5000; // 5 seconds

    if (!statusElement || !conversionId) return;

    const status = statusElement.textContent.trim();
    
    // Only auto-refresh for pending or processing statuses
    if (status === 'Pending' || status === 'Processing') {
        const autoRefresh = setInterval(function() {
            fetch(`/FileConversion/GetStatus/${conversionId}`)
                .then(response => response.json())
                .then(data => {
                    statusElement.textContent = data.status;
                    
                    // Update status badge class
                    const statusBadge = document.querySelector('.status-badge');
                    if (statusBadge) {
                        statusBadge.className = 'badge status-badge';
                        
                        switch(data.status) {
                            case 'Completed':
                                statusBadge.classList.add('bg-success');
                                clearInterval(autoRefresh);
                                showDownloadButton();
                                break;
                            case 'Failed':
                                statusBadge.classList.add('bg-danger');
                                clearInterval(autoRefresh);
                                showErrorMessage(data.errorMessage);
                                break;
                            case 'Processing':
                                statusBadge.classList.add('bg-warning');
                                break;
                            case 'Pending':
                                statusBadge.classList.add('bg-secondary');
                                break;
                        }
                    }
                    
                    // If status is no longer pending or processing, stop auto-refresh
                    if (data.status !== 'Pending' && data.status !== 'Processing') {
                        clearInterval(autoRefresh);
                        // Reload the page to show updated content
                        window.location.reload();
                    }
                })
                .catch(error => {
                    console.error('Error fetching conversion status:', error);
                });
        }, statusRefreshInterval);
    }

    function showDownloadButton() {
        const downloadSection = document.getElementById('downloadSection');
        if (downloadSection) {
            downloadSection.classList.remove('d-none');
        }
    }

    function showErrorMessage(message) {
        const errorSection = document.getElementById('errorSection');
        const errorMessage = document.getElementById('errorMessage');
        
        if (errorSection && errorMessage) {
            errorMessage.textContent = message || 'An error occurred during conversion.';
            errorSection.classList.remove('d-none');
        }
    }
}

/**
 * Search functionality for tables
 */
function initSearch() {
    const searchInput = document.getElementById('searchInput');
    const tableBody = document.querySelector('table tbody');
    
    if (!searchInput || !tableBody) return;
    
    searchInput.addEventListener('keyup', function() {
        const searchTerm = this.value.toLowerCase();
        const rows = tableBody.querySelectorAll('tr');
        
        rows.forEach(row => {
            const text = row.textContent.toLowerCase();
            if (text.includes(searchTerm)) {
                row.style.display = '';
            } else {
                row.style.display = 'none';
            }
        });
    });
}

/**
 * Date Range Pickers
 */
function initDateRangePickers() {
    const dateRangePicker = document.getElementById('dateRangePicker');
    
    if (!dateRangePicker) return;
    
    // Initialize date range picker if library is available
    if (typeof flatpickr !== 'undefined') {
        flatpickr(dateRangePicker, {
            mode: 'range',
            dateFormat: 'Y-m-d',
            maxDate: 'today'
        });
    }
}

/**
 * Confirmation dialogs using SweetAlert2
 */
function confirmDelete(url, itemName) {
    if (typeof Swal === 'undefined') {
        // Fallback to standard confirm if SweetAlert2 is not available
        if (confirm(`Are you sure you want to delete this ${itemName || 'item'}?`)) {
            window.location.href = url;
        }
        return;
    }
    
    Swal.fire({
        title: 'Are you sure?',
        text: `You are about to delete this ${itemName || 'item'}. This action cannot be undone.`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#dc3545',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
        if (result.isConfirmed) {
            window.location.href = url;
        }
    });
}

/**
 * Show success notifications using SweetAlert2
 */
function showSuccessNotification(message) {
    if (typeof Swal === 'undefined') {
        alert(message);
        return;
    }
    
    Swal.fire({
        title: 'Success!',
        text: message,
        icon: 'success',
        timer: 3000,
        showConfirmButton: false
    });
}

/**
 * Show error notifications using SweetAlert2
 */
function showErrorNotification(message) {
    if (typeof Swal === 'undefined') {
        alert(message);
        return;
    }
    
    Swal.fire({
        title: 'Error!',
        text: message,
        icon: 'error',
        confirmButtonColor: '#dc3545'
    });
}
