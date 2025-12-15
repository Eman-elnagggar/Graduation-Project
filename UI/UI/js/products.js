// ================================
// Product Safety Scanner JavaScript
// ================================

document.addEventListener('DOMContentLoaded', function() {
    
    // Tab switching
    const cameraOption = document.getElementById('cameraOption');
    const manualOption = document.getElementById('manualOption');
    const cameraScanner = document.getElementById('cameraScanner');
    const manualEntry = document.getElementById('manualEntry');

    if (cameraOption && manualOption) {
        cameraOption.addEventListener('click', function() {
            cameraOption.classList.add('active');
            manualOption.classList.remove('active');
            cameraScanner.style.display = 'block';
            manualEntry.style.display = 'none';
        });

        manualOption.addEventListener('click', function() {
            manualOption.classList.add('active');
            cameraOption.classList.remove('active');
            manualEntry.style.display = 'block';
            cameraScanner.style.display = 'none';
        });
    }

    // Camera scan button
    const startCamera = document.getElementById('startCamera');
    if (startCamera) {
        startCamera.addEventListener('click', function() {
            // Simulate camera activation
            const cameraFrame = document.querySelector('.camera-frame');
            cameraFrame.innerHTML = `
                <div class="scanning-animation">
                    <i class="fas fa-spinner fa-spin"></i>
                    <p>Scanning...</p>
                </div>
            `;
            
            // Simulate scan result after 2 seconds
            setTimeout(function() {
                showResult('safe');
            }, 2000);
        });
    }

    // Manual check button
    const checkProduct = document.getElementById('checkProduct');
    if (checkProduct) {
        checkProduct.addEventListener('click', function() {
            const barcode = document.getElementById('barcodeInput').value;
            const productName = document.getElementById('productName').value;
            
            if (!barcode && !productName) {
                alert('Please enter a barcode or product name');
                return;
            }

            // Simulate checking - show different results based on input
            if (productName.toLowerCase().includes('ibuprofen') || 
                productName.toLowerCase().includes('aspirin')) {
                showResult('danger');
            } else if (productName.toLowerCase().includes('tea') || 
                       productName.toLowerCase().includes('coffee')) {
                showResult('warning');
            } else {
                showResult('safe');
            }
        });
    }

    function showResult(type) {
        // Hide placeholder
        document.getElementById('resultsPlaceholder').style.display = 'none';
        
        // Hide all results first
        document.getElementById('safeResult').style.display = 'none';
        document.getElementById('dangerResult').style.display = 'none';
        document.getElementById('warningResult').style.display = 'none';

        // Show appropriate result
        if (type === 'safe') {
            document.getElementById('safeResult').style.display = 'block';
        } else if (type === 'danger') {
            document.getElementById('dangerResult').style.display = 'block';
        } else if (type === 'warning') {
            document.getElementById('warningResult').style.display = 'block';
        }

        // Scroll to results
        document.querySelector('.results-card').scrollIntoView({ 
            behavior: 'smooth',
            block: 'start'
        });
    }
});