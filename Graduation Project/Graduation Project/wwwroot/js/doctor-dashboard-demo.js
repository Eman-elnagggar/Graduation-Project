(() => {
  const root = document.querySelector("[data-doctor-dashboard]");
  if (!root) return;

  const dateEl = document.getElementById("ddCurrentDate");
  if (dateEl) {
    dateEl.textContent = new Date().toLocaleDateString("en-US", {
      weekday: "long",
      year: "numeric",
      month: "long",
      day: "numeric"
    });
  }

  const taskList = document.getElementById("tasksList");

  let tasks = [
    { text: "Review Sarah Ahmed CBC results", meta: "Due today · 9:00 AM", badge: "urgent", done: false },
    { text: "Sign Nour Hassan prescription", meta: "Due today · 11:30 AM", badge: "urgent", done: false },
    { text: "Update Mona Ibrahim care plan", meta: "Due today · After visit", badge: "normal", done: false },
    { text: "Call Dina Khalil re: glucose test", meta: "Done at 8:45 AM", badge: "done", done: true },
    { text: "Review weekly analytics report", meta: "Done at 8:00 AM", badge: "done", done: true },
    { text: "Confirm tomorrow's appointments", meta: "Done at 7:30 AM", badge: "done", done: true }
  ];

  const renderTasks = () => {
    if (!taskList) return;
    let doneCount = 0;
    taskList.innerHTML = tasks
      .map(
        (task, idx) => {
          if (task.done) doneCount++;
          const badgeText = task.badge === "urgent" ? "⚡ Urgent" : task.badge === "done" ? "✓ Done" : "Normal";
          return `
          <div class="task-item${task.done ? " done" : ""}" data-task-index="${idx}">
            <span class="task-check">${task.done ? '<i class="fas fa-check"></i>' : ""}</span>
            <div class="task-body">
              <div class="task-text">${task.text}</div>
              <div class="task-meta">${task.meta}</div>
            </div>
            <span class="task-badge ${task.badge}">${badgeText}</span>
          </div>`;
        }
      )
      .join("");

    const metaEl = document.getElementById("tasksMeta");
    if (metaEl) {
      metaEl.textContent = `${doneCount} of ${tasks.length} complete`;
    }
  };

  if (taskList) {
    taskList.addEventListener("click", (e) => {
      const item = e.target.closest("[data-task-index]");
      if (!item) return;
      const idx = Number(item.getAttribute("data-task-index"));
      if (Number.isNaN(idx) || !tasks[idx]) return;
      tasks[idx].done = !tasks[idx].done;
      tasks[idx].badge = tasks[idx].done ? "done" : tasks[idx].badge === "done" ? "normal" : tasks[idx].badge;
      tasks[idx].meta = tasks[idx].done ? `Done at ${new Date().toLocaleTimeString("en-US", { hour: "numeric", minute: "2-digit" })}` : tasks[idx].meta;
      renderTasks();
    });
  }

  root.querySelector("[data-add-task]")?.addEventListener("click", () => {
    const value = window.prompt("New task");
    if (!value || !value.trim()) return;
    tasks.unshift({ text: value.trim(), meta: "Added just now", badge: "normal", done: false });
    renderTasks();
  });

  renderTasks();
})();
