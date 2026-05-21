const monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

const chartColors = {
    blue: '#0C447C',
    lightBlue: '#378ADD',
    green: '#1D9E75',
    orange: '#E8913A',
    purple: '#7C3AED',
    red: '#DC3545',
    teal: '#0891B2',
    pink: '#EC4899'
};

document.addEventListener('DOMContentLoaded', function () {
    if (document.getElementById('hoursChart')) loadHoursChart();
    if (document.getElementById('revenueChart')) loadRevenueChart();
    if (document.getElementById('clientsChart')) loadClientsChart();
});

function loadHoursChart() {
    fetch('/Home/ChartHoursByMonth')
        .then(r => r.json())
        .then(data => {
            const labels = data.map(d => monthNames[d.month - 1] + ' ' + d.year);
            const values = data.map(d => d.hours);

            new Chart(document.getElementById('hoursChart'), {
                type: 'bar',
                data: {
                    labels: labels,
                    datasets: [{
                        label: 'Hours',
                        data: values,
                        backgroundColor: chartColors.blue,
                        borderRadius: 4
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: { legend: { display: false } },
                    scales: {
                        y: { beginAtZero: true, title: { display: true, text: 'Hours' } }
                    }
                }
            });
        });
}

function loadRevenueChart() {
    fetch('/Home/ChartRevenueByMonth')
        .then(r => r.json())
        .then(data => {
            const labels = data.map(d => monthNames[d.month - 1] + ' ' + d.year);
            const values = data.map(d => d.total);

            new Chart(document.getElementById('revenueChart'), {
                type: 'bar',
                data: {
                    labels: labels,
                    datasets: [{
                        label: 'Revenue (ZAR)',
                        data: values,
                        backgroundColor: chartColors.green,
                        borderRadius: 4
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: { legend: { display: false } },
                    scales: {
                        y: { beginAtZero: true, title: { display: true, text: 'ZAR' } }
                    }
                }
            });
        });
}

function loadClientsChart() {
    fetch('/Home/ChartHoursByClient')
        .then(r => r.json())
        .then(data => {
            const labels = data.map(d => d.client);
            const values = data.map(d => d.hours);
            const colors = Object.values(chartColors).slice(0, data.length);

            new Chart(document.getElementById('clientsChart'), {
                type: 'doughnut',
                data: {
                    labels: labels,
                    datasets: [{
                        data: values,
                        backgroundColor: colors
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: { position: 'bottom' }
                    }
                }
            });
        });
}