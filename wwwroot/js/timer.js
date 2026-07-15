/* ============================================================
   TracKeee Timer Widget
   Collapsed clock icon → expands to setup panel
   → Start → collapses to running badge (icon + time + stop)
   ============================================================ */

let timerInterval = null;
let timerStartedAt = null;
let timerRunning   = false;

document.addEventListener('DOMContentLoaded', function () {
    renderWidget();
    checkTimerStatus();
});

/* ── Backend calls ─────────────────────────────────────────── */

function checkTimerStatus() {
    fetch('/Timer/Status')
        .then(r => r.json())
        .then(data => {
            if (data.running) {
                timerStartedAt = new Date(data.startedAt);
                timerRunning = true;
                setRunningState(data.projectName || '', data.clientName || '', data.description || '');
                startTicking();
            } else {
                timerRunning = false;
                setIdleState();
                loadProjects();
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
    const projectSel = document.getElementById('timerProjectSelect');
    const descInput  = document.getElementById('timerDescription');
    const projectId  = projectSel.value;
    const description = descInput.value;

    if (!projectId) {
        projectSel.focus();
        projectSel.style.borderColor = 'var(--color-danger)';
        setTimeout(() => { projectSel.style.borderColor = ''; }, 1500);
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
                timerRunning = true;
                const projectName = projectSel.options[projectSel.selectedIndex].text;
                setRunningState(projectName, '', description);
                startTicking();
                closeDropdown();
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
                timerRunning = false;
                document.title = document.title.replace(/^\d{2}:\d{2}:\d{2} — /, '');
                if (window.location.pathname === '/' || window.location.pathname.startsWith('/TimeEntries')) {
                    location.reload();
                } else {
                    setIdleState();
                    loadProjects();
                }
            }
        });
}

/* ── Ticking ───────────────────────────────────────────────── */

function startTicking() {
    if (timerInterval) clearInterval(timerInterval);
    updateTimerDisplay();
    timerInterval = setInterval(updateTimerDisplay, 1000);
}

function updateTimerDisplay() {
    if (!timerStartedAt) return;
    const diff = Math.floor((new Date() - timerStartedAt) / 1000);
    const h = Math.floor(diff / 3600).toString().padStart(2, '0');
    const m = Math.floor((diff % 3600) / 60).toString().padStart(2, '0');
    const s = (diff % 60).toString().padStart(2, '0');
    const display = `${h}:${m}:${s}`;

    const collapsed = document.getElementById('timerBadgeTime');
    const expanded  = document.getElementById('timerDisplay');
    if (collapsed) collapsed.textContent = display;
    if (expanded)  expanded.textContent  = display;

    document.title = document.title.replace(/^\d{2}:\d{2}:\d{2} — /, '');
    document.title = `${display} — ${document.title}`;
}

/* ── Widget rendering ──────────────────────────────────────── */

function renderWidget() {
    const widget = document.getElementById('timerWidget');
    if (!widget) return;

    widget.innerHTML = `
        <div class="timer">
            <!-- Trigger: clock icon (idle) OR pulsing badge + time (running) -->
            <button type="button"
                    id="timerTrigger"
                    class="timer__trigger"
                    aria-haspopup="true"
                    aria-expanded="false"
                    aria-label="Time tracker">
                <span id="timerTriggerContent">
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                         stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                        <circle cx="12" cy="12" r="10"/>
                        <polyline points="12 6 12 12 16 14"/>
                    </svg>
                </span>
            </button>

            <!-- Dropdown -->
            <div id="timerDropdown" class="timer__dropdown" role="dialog" aria-label="Time tracker">
                <!-- Idle content -->
                <div id="timerIdle">
                    <div class="form__group" style="margin-bottom:var(--space-3);">
                        <label class="form__label" for="timerProjectSelect">Project</label>
                        <select id="timerProjectSelect" class="form__select">
                            <option value="">Select project...</option>
                        </select>
                    </div>
                    <div class="form__group" style="margin-bottom:var(--space-3);">
                        <label class="form__label" for="timerDescription">Description</label>
                        <input id="timerDescription" type="text" class="form__input" placeholder="What are you working on?" />
                    </div>
                    <button type="button" onclick="startTimer()" class="btn btn--primary btn--full">
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                            <polygon points="5 3 19 12 5 21 5 3"/>
                        </svg>
                        Start Timer
                    </button>
                </div>

                <!-- Running content -->
                <div id="timerActive" style="display:none;">
                    <div class="timer__label" id="timerActiveProject">—</div>
                    <div class="timer__label" id="timerActiveDesc" style="color:var(--color-text-secondary);margin-top:var(--space-1);"></div>
                    <div class="timer__running" style="margin-top:var(--space-3);">
                        <div class="timer__display" id="timerDisplay" aria-live="polite">00:00:00</div>
                        <button type="button" onclick="stopTimer()" class="btn btn--danger">
                            <svg width="12" height="12" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                                <rect x="4" y="4" width="16" height="16" rx="2"/>
                            </svg>
                            Stop
                        </button>
                    </div>
                </div>
            </div>
        </div>
    `;

    /* ── Dropdown open/close ── */
    const trigger  = document.getElementById('timerTrigger');
    const dropdown = document.getElementById('timerDropdown');

    trigger.addEventListener('click', function (e) {
        e.stopPropagation();
        const isOpen = dropdown.classList.contains('timer__dropdown--visible');
        if (isOpen) closeDropdown();
        else openDropdown();
    });

    document.addEventListener('click', function (e) {
        if (!dropdown.classList.contains('timer__dropdown--visible')) return;
        if (!trigger.contains(e.target) && !dropdown.contains(e.target)) closeDropdown();
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && dropdown.classList.contains('timer__dropdown--visible')) {
            closeDropdown();
            trigger.focus();
        }
    });
}

function openDropdown() {
    const trigger  = document.getElementById('timerTrigger');
    const dropdown = document.getElementById('timerDropdown');
    dropdown.classList.add('timer__dropdown--visible');
    trigger.setAttribute('aria-expanded', 'true');

    /* Focus first meaningful element */
    if (timerRunning) {
        const stopBtn = dropdown.querySelector('.btn--danger');
        if (stopBtn) stopBtn.focus();
    } else {
        const first = dropdown.querySelector('select, input, button');
        if (first) first.focus();
    }
}

function closeDropdown() {
    const trigger  = document.getElementById('timerTrigger');
    const dropdown = document.getElementById('timerDropdown');
    dropdown.classList.remove('timer__dropdown--visible');
    trigger.setAttribute('aria-expanded', 'false');
}

/* ── State setters ─────────────────────────────────────────── */

function setIdleState() {
    const triggerContent = document.getElementById('timerTriggerContent');
    const trigger        = document.getElementById('timerTrigger');
    const idle           = document.getElementById('timerIdle');
    const active         = document.getElementById('timerActive');

    trigger.classList.remove('timer__trigger--active');
    trigger.setAttribute('aria-label', 'Time tracker');
    triggerContent.innerHTML = `
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"
             stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
            <circle cx="12" cy="12" r="10"/>
            <polyline points="12 6 12 12 16 14"/>
        </svg>
    `;
    idle.style.display   = 'block';
    active.style.display = 'none';
}

function setRunningState(projectName, clientName, description) {
    const triggerContent = document.getElementById('timerTriggerContent');
    const trigger        = document.getElementById('timerTrigger');
    const idle           = document.getElementById('timerIdle');
    const active         = document.getElementById('timerActive');
    const activeProj     = document.getElementById('timerActiveProject');
    const activeDesc     = document.getElementById('timerActiveDesc');

    trigger.classList.add('timer__trigger--active');
    trigger.setAttribute('aria-label', 'Timer running. Click to view.');
    triggerContent.innerHTML = `
        <span class="timer__badge-dot" aria-hidden="true"></span>
        <span class="timer__badge-time" id="timerBadgeTime">00:00:00</span>
    `;

    activeProj.textContent = projectName || 'Untitled project';
    activeDesc.textContent = description ? `"${description}"` : '';
    idle.style.display   = 'none';
    active.style.display = 'block';
}
