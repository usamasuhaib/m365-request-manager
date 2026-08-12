const fs = require('fs');
const path = require('path');

const colorBase64 = 'iVBORw0KGgoAAAANSUhEUgAAAGAAAABgCAYAAADimHc4AAAABmJLR0QA/wD/AP+gvaeTAAAAI0lEQVR4nO3BMQEAAADCoPVPbQwfoAAAAAAAAAAAAAAAAAAAAIC3AYb2AAFE489FAAAAAElFTkSuQmCC';
const outlineBase64 = 'iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAABmJLR0QA/wD/AP+gvaeTAAAAI0lEQVR4nO3BMQEAAADCoPVPbQwfoAAAAAAAAAAAAAAAAAAAAIC3AYb2AAFE489FAAAAAElFTkSuQmCC';

const manifestDir = path.join(__dirname, '..', '..', 'manifest');
if (!fs.existsSync(manifestDir)){
    fs.mkdirSync(manifestDir, { recursive: true });
}

fs.writeFileSync(path.join(manifestDir, 'color.png'), Buffer.from(colorBase64, 'base64'));
fs.writeFileSync(path.join(manifestDir, 'outline.png'), Buffer.from(outlineBase64, 'base64'));
console.log('Icons generated successfully in manifest folder.');
