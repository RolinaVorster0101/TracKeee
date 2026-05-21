let timerInterval = null;
let timerStartedAt = null;

document.addEventListener('DOMContentLoaded', function () {
    checkTimerStatus();
    loadProjects();
});

function checkTimerStatus() {
    fetch('/Timer/Status')
        .then(r => r.json())
        .then(data => {
            if (data.running) {
                timerStartedAt = new Date(data.startedAt);
                showRunningTimer(data.projectName, data.clientName, data.description);
                startTicking();
            } else {
                showStartForm();
            }
        });
}

function loadProjects() {
    fetch('/Timer/Projects')
        .then(r => r.json())
        .then(projects => {
            const select = document.getElementById('timerProjectSelect');
            if (!select) return;
            select.innerHTML = '<option value="">Select project...</option>';
            projects.forEach(p => {
                const opt = document.createElement('option');
                opt.value = p.id;
                opt.textContent = p.name;
                select.appendChild(opt);
            });
        });
}

function startTimer() {
    const projectId = document.getElementById('timerProjectSelect').value;
    const description = document.getElementById('timerDescription').value;

    if (!projectId) {
        alert('Please select a project');
        return;
    }

    fetch('/Timer/Start', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ projectId: parseInt(projectId), description: description })
    })
        .then(r => r.json())
        .then(data => {
            if (data.success) {
                timerStartedAt = new Date();
                const select = document.getElementById('timerProjectSelect');
                const projectName = select.options[select.selectedIndex].text;
                showRunningTimer(projectName, '', description);
                startTicking();
            }
        });
}

function stopTimer() {
    fetch('/Timer/Stop', { method: 'POST' })
        .then(r => r.json())
        .then(data => {
            if (data.success) {
                clearInterval(timerInterval);
                timerInterval = null;
                timerStartedAt = null;
                showStartForm();
                // Reload page to update dashboard/time entries
                if (window.location.pathname === '/' || window.location.pathname.startsWith('/TimeEntries')) {
                    location.reload();
                }
            }
        });
}

function startTicking() {
    if (timerInterval) clearInterval(timerInterval);
    updateTimerDisplay();
    timerInterval = setInterval(updateTimerDisplay, 1000);
}

function updateTimerDisplay() {
    if (!timerStartedAt) return;
    const now = new Date();
    const diff = Math.floor((now - timerStartedAt) / 1000);
    const hours = Math.floor(diff / 3600);
    const mins = Math.floor((diff % 3600) / 60);
    const secs = diff % 60;
    const display = `${hours.toString().padStart(2, '0')}:${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;

    const el = document.getElementById('timerDisplay');
    if (el) el.textContent = display;

    // Also update page title
    document.title = `${display} — TracKeee`;
}

function showRunningTimer(projectName, clientName, description) {
    const widget = document.getElementById('timerWidget');
    if (!widget) return;
    widget.innerHTML = `
        <div class="d-flex align-items-center gap-2">
            <span class="badge bg-danger">●</span>
            <span class="small text-muted">${projectName || ''}</span>
            <strong id="timerDisplay" class="text-danger">00:00:00</strong>
            <button onclick="stopTimer()" class="btn btn-sm btn-outline-danger">Stop</button>
        </div>
    `;
}

function showStartForm() {
    const widget = document.getElementById('timerWidget');
    if (!widget) return;
    widget.innerHTML = `
        <div class="d-flex align-items-center gap-2">
            <select id="timerProjectSelect" class="form-select form-select-sm" style="width: 200px;">
                <option value="">Select project...</option>
            </select>
            <input id="timerDescription" type="text" class="form-control form-control-sm" placeholder="What are you working on?" style="width: 200px;" />
            <button onclick="startTimer()" class="btn btn-sm btn-primary">Start</button>
        </div>
    `;
    loadProjects();
}