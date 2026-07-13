let currentJobId = null;
let currentFormat = 'mp4';

const connection = new signalR.HubConnectionBuilder()
    .withUrl('/hub/download')
    .withAutomaticReconnect()
    .build();

connection.on('DownloadStatusUpdated', (data) => {
    updateProgress(data);
    updateHistoryItem(data);
});

connection.start().catch(err => console.error('SignalR error:', err));

document.getElementById('infoForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    const url = document.getElementById('urlInput').value.trim();
    if (!url) return;

    const btn = e.target.querySelector('button');
    btn.disabled = true;
    btn.textContent = 'Mencari...';

    try {
        const res = await fetch(`/api/video/info?url=${encodeURIComponent(url)}`);
        if (!res.ok) throw new Error('Gagal mendapatkan info video');

        const data = await res.json();
        showVideoInfo(data);
        document.getElementById('downloadUrl').value = url;
    } catch (err) {
        alert('Error: ' + err.message);
    } finally {
        btn.disabled = false;
        btn.textContent = 'Cari Info';
    }
});

document.getElementById('downloadForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    const url = document.getElementById('downloadUrl').value;
    const format = document.querySelector('input[name="format"]:checked').value;
    currentFormat = format;

    const btn = document.getElementById('startDownloadBtn');
    btn.disabled = true;
    btn.textContent = 'Memulai...';

    try {
        const res = await fetch('/api/download/start', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ url, format, quality: 'best' })
        });

        if (!res.ok) throw new Error('Gagal memulai download');

        const data = await res.json();
        currentJobId = data.id;

        showProgress(data.id);
        connection.invoke('JoinJobGroup', data.id).catch(err => console.error(err));

        addHistoryItem({
            id: data.id,
            url,
            format,
            status: 'Queued',
            progress: 0
        });

        btn.textContent = 'Downloading...';
    } catch (err) {
        alert('Error: ' + err.message);
        btn.disabled = false;
        btn.textContent = 'Download';
    }
});

document.getElementById('deleteFileBtn').addEventListener('click', async () => {
    if (!currentJobId) return;

    if (!confirm('Hapus file ini?')) return;

    try {
        const res = await fetch(`/api/download/file/${currentJobId}`, {
            method: 'DELETE'
        });

        if (!res.ok) throw new Error('Gagal menghapus file');

        document.getElementById('downloadFileBtn').classList.add('hidden');
        document.getElementById('deleteFileBtn').classList.add('hidden');
        document.getElementById('progressMessage').textContent = 'File dihapus.';
    } catch (err) {
        alert('Error: ' + err.message);
    }
});

function showVideoInfo(data) {
    document.getElementById('infoSection').classList.remove('hidden');
    document.getElementById('thumbnail').src = data.thumbnail || '';
    document.getElementById('videoTitle').textContent = data.title || 'Unknown';
    document.getElementById('videoUploader').textContent = data.uploader || '';
    document.getElementById('videoDuration').textContent = formatDuration(data.duration);
}

function formatDuration(seconds) {
    if (!seconds) return '';
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m}:${s.toString().padStart(2, '0')}`;
}

function showProgress(jobId) {
    document.getElementById('progressSection').classList.remove('hidden');
    document.getElementById('progressPercent').textContent = '0%';
    document.getElementById('progressFill').style.width = '0%';
    document.getElementById('progressMessage').textContent = 'Menunggu antrian...';
    document.getElementById('downloadFileBtn').classList.add('hidden');
    document.getElementById('deleteFileBtn').classList.add('hidden');
    document.getElementById('errorMessage').classList.add('hidden');
}

function updateProgress(data) {
    document.getElementById('progressPercent').textContent = `${data.progress || 0}%`;
    document.getElementById('progressFill').style.width = `${data.progress || 0}%`;
    document.getElementById('progressMessage').textContent = data.lastMessage || '';

    if (data.status === 'Completed') {
        document.getElementById('startDownloadBtn').disabled = false;
        document.getElementById('startDownloadBtn').textContent = 'Download';

        document.getElementById('downloadFileBtn').href = `/api/download/file/${currentJobId}`;
        document.getElementById('downloadFileBtn').classList.remove('hidden');
        document.getElementById('deleteFileBtn').classList.remove('hidden');
    }

    if (data.status === 'Failed') {
        document.getElementById('startDownloadBtn').disabled = false;
        document.getElementById('startDownloadBtn').textContent = 'Download';
        document.getElementById('errorMessage').textContent = data.error || 'Download gagal.';
        document.getElementById('errorMessage').classList.remove('hidden');
    }
}

function addHistoryItem(data) {
    const list = document.getElementById('historyList');
    const item = document.createElement('div');
    item.className = 'history-item';
    item.id = `history-${data.id}`;

    item.innerHTML = `
        <span class="title">${data.url}</span>
        <span class="status ${data.status.toLowerCase()}">${data.status}</span>
    `;

    list.prepend(item);
}

function updateHistoryItem(data) {
    const item = document.getElementById(`history-${data.id}`);
    if (!item) {
        addHistoryItem(data);
        return;
    }

    const statusEl = item.querySelector('.status');
    statusEl.textContent = data.status;
    statusEl.className = `status ${data.status.toLowerCase()}`;
}

async function loadHistory() {
    // Load history bisa ditambahkan nanti dengan endpoint GET /api/downloads
}
