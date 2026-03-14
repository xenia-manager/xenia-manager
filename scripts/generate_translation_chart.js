const fs = require('fs');
const { createCanvas } = require('canvas');

// Read translation data from stdin
let translationData;
try {
    const inputData = fs.readFileSync(0, 'utf-8');
    translationData = JSON.parse(inputData);
} catch (error) {
    console.error('[FATAL] Failed to read translation data from stdin');
    process.exit(1);
}

const { TotalStrings, Translations } = translationData;

function drawRoundedRect(ctx, x, y, width, height, radius) {
    ctx.beginPath();
    ctx.moveTo(x + radius, y);
    ctx.lineTo(x + width - radius, y);
    ctx.quadraticCurveTo(x + width, y, x + width, y + radius);
    ctx.lineTo(x + width, y + height - radius);
    ctx.quadraticCurveTo(x + width, y + height, x + width - radius, y + height);
    ctx.lineTo(x + radius, y + height);
    ctx.quadraticCurveTo(x, y + height, x, y + height - radius);
    ctx.lineTo(x, y + radius);
    ctx.quadraticCurveTo(x, y, x + radius, y);
    ctx.closePath();
}

function getHSL(hue, saturation, lightness) {
    return `hsl(${hue}, ${saturation}%, ${lightness}%)`;
}

async function generateTranslationChart(translations, totalStrings) {
    const width = 900;
    const height = 600;
    const canvas = createCanvas(width, height);
    const ctx = canvas.getContext('2d');

    // Background
    ctx.fillStyle = '#181A1B';
    ctx.fillRect(0, 0, width, height);

    // Chart margins
    const chartLeft = 80;
    const chartRight = width - 40;
    const chartTop = 80;
    const chartBottom = height - 60;
    const chartWidth = chartRight - chartLeft;
    const chartHeight = chartBottom - chartTop;

    // Title
    ctx.fillStyle = '#fff';
    ctx.font = 'bold 22px Arial';
    ctx.textAlign = 'center';
    ctx.fillText('Xenia Manager Translation Progress', width / 2, 35);

    // Subtitle
    ctx.fillStyle = '#ccc';
    ctx.font = '16px Arial';
    ctx.fillText(`Total strings to translate: ${totalStrings}`, width / 2, 55);

    // Y-axis label
    ctx.save();
    ctx.translate(20, chartTop + chartHeight / 2);
    ctx.rotate(-Math.PI / 2);
    ctx.fillStyle = '#fff';
    ctx.font = 'bold 14px Arial';
    ctx.textAlign = 'center';
    ctx.fillText('Completion Percentage (%)', 0, 0);
    ctx.restore();

    // X-axis label
    ctx.fillStyle = '#fff';
    ctx.font = 'bold 14px Arial';
    ctx.textAlign = 'center';
    ctx.fillText('Languages', chartLeft + chartWidth / 2, height - 15);

    // Sort languages alphabetically
    const sortedLangs = Object.keys(translations).sort();
    const barCount = sortedLangs.length;
    const barGap = 10;
    const barWidth = (chartWidth - (barCount + 1) * barGap) / barCount;

    // Draw Y-axis grid lines and labels
    ctx.strokeStyle = 'rgba(255,255,255,0.08)';
    ctx.lineWidth = 1;
    ctx.fillStyle = '#fff';
    ctx.font = '14px Arial';
    ctx.textAlign = 'right';

    for (let i = 0; i <= 5; i++) {
        const y = chartBottom - (i / 5) * chartHeight;
        ctx.beginPath();
        ctx.moveTo(chartLeft, y);
        ctx.lineTo(chartRight, y);
        ctx.stroke();

        const label = (i * 20).toString();
        ctx.fillText(label, chartLeft - 10, y + 5);
    }

    // Draw bars
    sortedLangs.forEach((lang, index) => {
        const data = translations[lang];
        const percentage = data.Percentage;
        const barHeight = (percentage / 100) * chartHeight;
        const x = chartLeft + barGap + index * (barWidth + barGap);
        const y = chartBottom - barHeight;

        // Generate vibrant color
        const hue = (index * 137.508) % 360;
        const color = getHSL(hue, 80, 55);

        // Draw rounded bar
        ctx.fillStyle = color;
        drawRoundedRect(ctx, x, y, barWidth, barHeight, 6);
        ctx.fill();

        // Draw border
        ctx.strokeStyle = '#eee';
        ctx.lineWidth = 2;
        ctx.stroke();

        // Draw label (percentage or count)
        ctx.font = 'bold 14px Arial';
        ctx.textAlign = 'center';

        if (percentage > 15) {
            // White text inside bar
            ctx.fillStyle = '#fff';
            ctx.fillText(`${data.Translated}/${data.Total}`, x + barWidth / 2, y + barHeight / 2 + 5);
        } else {
            // Black text above bar
            ctx.fillStyle = '#000';
            ctx.fillText(`${percentage}%`, x + barWidth / 2, y - 5);
        }

        // Draw language name on X-axis
        ctx.save();
        ctx.translate(x + barWidth / 2, chartBottom + 15);
        ctx.rotate(-45 * Math.PI / 180);
        ctx.fillStyle = '#fff';
        ctx.textAlign = 'right';
        ctx.fillText(lang, 0, 0);
        ctx.restore();
    });

    // Save chart
    if (!fs.existsSync('assets')) {
        fs.mkdirSync('assets');
    }

    const buffer = canvas.toBuffer('image/png');
    fs.writeFileSync('assets/translation-progress.png', buffer);
    console.log('Translation chart generated: assets/translation-progress.png');
}

generateTranslationChart(Translations, TotalStrings)
    .then(() => {
        console.log('Chart generation completed successfully');
        process.exit(0);
    })
    .catch(err => {
        console.error('[FATAL] Error generating chart:', err);
        process.exit(1);
    });
