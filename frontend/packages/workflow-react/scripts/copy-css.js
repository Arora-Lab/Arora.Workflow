const fs = require('fs');
const path = require('path');

const src = path.join(__dirname, '../src/styles.css');
const dest = path.join(__dirname, '../dist/styles.css');

fs.mkdirSync(path.dirname(dest), { recursive: true });
fs.copyFileSync(src, dest);
console.log('Copied styles.css to dist/styles.css');
