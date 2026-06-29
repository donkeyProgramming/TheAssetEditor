const documentationPages = [
    { title: 'Installing', fileName: 'Installing.html' },
    { title: 'Getting Started', fileName: 'GettingStarted.html' },
    { title: 'Camera Control', fileName: 'CameraControl.html' },
    { title: 'PackFile Handling', fileName: 'PackFileHandling.html' },
    { title: 'PackFile Handling (Updated)', fileName: 'packfilehandling2.html' },
    {
        title: 'Kitbashing',
        children: [
            { title: 'Basics', fileName: 'Kitbash_Basics.html' },
            { title: 'Mesh Fitter', fileName: 'Kitbash_MeshFitter.html' },
            { title: 'Pin Tool', fileName: 'Kitbash_PinTool.html' },
            { title: 'Photo Studio', fileName: 'Kitbash_PhotoStudio.html' }
        ]
    }
];

document.addEventListener('DOMContentLoaded', () => {
    initializeDocumentationLayout();
    initializeImageOverlay();
});

function initializeDocumentationLayout() {
    if (!document.body || document.body.dataset.documentationLayout === 'true') {
        return;
    }

    const currentPage = getCurrentFileName();
    const originalChildren = Array.from(document.body.childNodes);
    const shell = document.createElement('div');
    shell.className = 'doc-shell';

    const sidebar = buildSidebar(currentPage);
    const content = document.createElement('main');
    content.className = 'doc-content';

    const article = document.createElement('article');
    article.className = 'doc-article';

    for (const child of originalChildren) {
        article.appendChild(child);
    }

    content.appendChild(article);
    shell.append(sidebar, content);
    document.body.replaceChildren(shell);
    document.body.dataset.documentationLayout = 'true';

    const activePage = findPage(currentPage);
    if (activePage) {
        document.title = `Asset Editor Documentation - ${activePage.title}`;
    }
}

function buildSidebar(currentPage) {
    const sidebar = document.createElement('aside');
    sidebar.className = 'doc-sidebar';

    const headerLink = document.createElement('a');
    headerLink.className = 'doc-sidebar-header';
    headerLink.href = 'Installing.html';
    headerLink.textContent = 'Asset Editor Documentation';

    const icon = document.createElement('img');
    icon.src = '../icon.svg';
    icon.alt = '';
    headerLink.appendChild(icon);
    sidebar.appendChild(headerLink);

    const nav = document.createElement('nav');
    nav.className = 'doc-nav';
    renderNavigationItems(nav, documentationPages, currentPage, 0);
    sidebar.appendChild(nav);

    return sidebar;
}

function renderNavigationItems(container, items, currentPage, level) {
    for (const item of items) {
        if (Array.isArray(item.children)) {
            const section = document.createElement('div');
            section.className = 'doc-nav-section';
            section.textContent = item.title;
            section.style.paddingLeft = `${level * 12}px`;

            if (item.children.some(child => isCurrentPage(child.fileName, currentPage))) {
                section.classList.add('active');
            }

            container.appendChild(section);
            renderNavigationItems(container, item.children, currentPage, level + 1);
            continue;
        }

        const link = document.createElement('a');
        link.className = 'doc-nav-link';
        link.textContent = item.title;
        link.href = item.fileName;
        link.style.paddingLeft = `${level * 12}px`;

        if (isCurrentPage(item.fileName, currentPage)) {
            link.classList.add('active');
            link.setAttribute('aria-current', 'page');
        }

        container.appendChild(link);
    }
}

function getCurrentFileName() {
    const path = window.location.pathname || '';
    const parts = path.split('/');
    return (parts[parts.length - 1] || '').toLowerCase();
}

function isCurrentPage(fileName, currentPage) {
    return fileName.toLowerCase() === currentPage;
}

function findPage(currentPage) {
    for (const item of documentationPages) {
        if (Array.isArray(item.children)) {
            const child = item.children.find(entry => isCurrentPage(entry.fileName, currentPage));
            if (child) {
                return child;
            }

            continue;
        }

        if (isCurrentPage(item.fileName, currentPage)) {
            return item;
        }
    }

    return null;
}

function initializeImageOverlay() {
    const docImages = document.querySelectorAll('.doc-image');
    let activeOverlay = null;

    function closeOverlay() {
        if (activeOverlay) {
            activeOverlay.remove();
            activeOverlay = null;
        }

        document.removeEventListener('keydown', onKeyDown);
    }

    function onKeyDown(event) {
        if (event.key === 'Escape') {
            closeOverlay();
        }
    }

    function openOverlay(sourceImage) {
        closeOverlay();

        const overlay = document.createElement('div');
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

        const overlayImage = document.createElement('img');
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
        document.body.appendChild(overlay);
        document.addEventListener('keydown', onKeyDown);
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
}
