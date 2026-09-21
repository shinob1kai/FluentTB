// Deterministic raster export from the editable SVG; no image generation service.
const fs = require('node:fs');
const path = require('node:path');
const { Resvg } = require('@resvg/resvg-js');
const svg = fs.readFileSync(path.join(__dirname, 'FluentTB.svg'), 'utf8');
const result = new Resvg(svg, { fitTo: { mode: 'width', value: 1280 } }).render();
fs.writeFileSync(path.join(__dirname, 'FluentTB-master.png'), result.asPng());
