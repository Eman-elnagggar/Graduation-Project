// Analytics JavaScript
document.addEventListener("DOMContentLoaded", function () {
  initDemoData();
  initSidebar();
  initNotifications();
  initPeriodSelector();
  initExportButton();
});

// Initialize period selector
function initPeriodSelector() {
  document
    .getElementById("periodSelect")
    ?.addEventListener("change", function () {
      // In production, this would fetch new data based on selected period
      showToast(`Showing data for last ${this.value} days`, "info");
      // Simulate data refresh
      animateCharts();
    });
}

// Initialize export button
function initExportButton() {
  document
    .getElementById("exportReportBtn")
    ?.addEventListener("click", function () {
      showToast("Report export feature coming soon", "info");
    });
}

// Animate charts on period change (demo)
function animateCharts() {
  // Animate bar chart
  const bars = document.querySelectorAll(".bar-chart .bar");
  bars.forEach((bar) => {
    const currentHeight = bar.style.height;
    bar.style.height = "0%";
    setTimeout(() => {
      bar.style.height = currentHeight;
    }, 100);
  });

  // Animate horizontal bars
  const horizontalBars = document.querySelectorAll(".bar-fill");
  horizontalBars.forEach((bar) => {
    const currentWidth = bar.style.width;
    bar.style.width = "0%";
    setTimeout(() => {
      bar.style.width = currentWidth;
    }, 100);
  });
}

// Add CSS transition for charts
document.addEventListener("DOMContentLoaded", function () {
  const style = document.createElement("style");
  style.textContent = `
        .bar-chart .bar,
        .bar-fill {
            transition: all 0.5s ease-out;
        }
    `;
  document.head.appendChild(style);
});
