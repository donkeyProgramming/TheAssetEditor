document.addEventListener('DOMContentLoaded', () => {
    const docImages = document.querySelectorAll('.doc-image');
    let activeOverlay = null;

    function getTargetDocument() {
        try {
            if (window.top && window.top.document) {
                return window.top.document;
            }
        } catch {
            // Fall back to the current document when top is not accessible.
        }

        return document;
    }

    function closeOverlay() {
        const targetDocument = getTargetDocument();

        if (activeOverlay) {
            activeOverlay.remove();
            activeOverlay = null;
        }

        targetDocument.removeEventListener('keydown', onKeyDown);
    }

    function onKeyDown(event) {
        if (event.key === 'Escape') {
            closeOverlay();
        }
    }

    function openOverlay(sourceImage) {
        const targetDocument = getTargetDocument();
        closeOverlay();

        const overlay = targetDocument.createElement('div');
        overlay.style.position = 'fixed';
        overlay.style.inset = '0';
        overlay.style.background = 'rgba(0, 0, 0, 0.92)';
        overlay.style.display = 'flex';
        overlay.style.alignItems = 'center';
        overlay.style.justifyContent = 'center';
        overlay.style.padding = '24px';
        overlay.style.boxSizing = 'border-box';
        overlay.style.zIndex = '2147483647';
        overlay.style.cursor = 'zoom-out';

        const overlayImage = targetDocument.createElement('img');
        overlayImage.src = sourceImage.currentSrc || sourceImage.src;
        overlayImage.alt = sourceImage.alt || '';
        overlayImage.style.maxWidth = '100%';
        overlayImage.style.maxHeight = '100%';
        overlayImage.style.objectFit = 'contain';
        overlayImage.style.border = 'none';
        overlayImage.style.boxShadow = '0 8px 30px rgba(0, 0, 0, 0.45)';
        overlayImage.style.background = '#000';
        overlayImage.style.cursor = 'auto';

        overlay.addEventListener('click', closeOverlay);
        overlayImage.addEventListener('click', (event) => {
            event.stopPropagation();
        });

        overlay.appendChild(overlayImage);
        targetDocument.body.appendChild(overlay);
        targetDocument.addEventListener('keydown', onKeyDown);
        activeOverlay = overlay;
    }

    for (const image of docImages) {
        if (!image.getAttribute('title')) {
            image.setAttribute('title', 'Click to enlarge');
        }

        image.addEventListener('click', () => {
            openOverlay(image);
        });
    }
});
