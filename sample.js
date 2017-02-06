var edge = require('electron-edge');

// initialize World API
var worldApi = edge.func('native_libraries/world.csx')(null, true);

var fileInstance = worldApi.initFromFile(
    '/Users/yamachu/Project/CPP/World/test/vaiueo2d.wav', true);

if (fileInstance === null) {
    console.log("Error when reading file: file not exist or not monophonic");
    process.exit(1);
}

var fileInfo = fileInstance.getFileInfo(null, true);

fileInstance.analysis(null, true);
console.log('Finish Analysis');
console.log('File save: ', fileInstance.saveToFile({
    fileName: '/Users/yamachu/ana_syn.wav'
}, true));

fileInstance.synthesis(null, true);
console.log('Finish Synthesis');
console.log('File save: ', fileInstance.saveToFile({
    fileName: '/Users/yamachu/ana_syn.wav'
}, true));

fileInstance.synthesis(null, true);
console.log('Finish Synthesis');
// in this save phase, saved file name will not be ana_syn.wav, 
// because overwrite parameter is false
console.log('File save: ', fileInstance.saveToFile({
    fileName: '/Users/yamachu/ana_syn.wav',
    overwrite: false
}, true));

var update = {};

console.log('initialize');
var base_f0 = fileInstance.getF0(null, true);
console.dir(base_f0.slice(0,5));

for(var i = 0; i < base_f0.length; i++) {
    update[i + ''] = [base_f0[i], 0];
}
console.log('Fill 0');
fileInstance.updateF0Points(update, true);
var fill_0 = fileInstance.getF0(null, true);
console.dir(fill_0.slice(0,5));

for(var i = 0; i < base_f0.length; i++) {
    update[i + ''] = [fill_0[i], 1];
}
console.log('Fill 1');
fileInstance.updateF0Points(update, true);
var fill_1 = fileInstance.getF0(null, true);
console.dir(fill_1.slice(0,5));

for(var i = 0; i < base_f0.length; i++) {
    update[i + ''] = [fill_1[i], 2];
}
console.log('Fill 2');
fileInstance.updateF0Points(update, true);
var fill_2 = fileInstance.getF0(null, true);
console.dir(fill_2.slice(0,5));

for(var i = 0; i < base_f0.length; i++) {
    update[i + ''] = [fill_2[i], 3];
}
console.log('Fill 3');
fileInstance.updateF0Points(update, true);
var fill_3 = fileInstance.getF0(null, true);
console.dir(fileInstance.getF0(null, true).slice(0,5));

for(var i = 0; i < base_f0.length; i++) {
    update[i + ''] = [fill_3[i], 4];
}
console.log('Fill 4');
fileInstance.updateF0Points(update, true);
var fill_4 = fileInstance.getF0(null, true);
console.dir(fileInstance.getF0(null, true).slice(0,5));

for(var i = 0; i < base_f0.length; i++) {
    update[i + ''] = [fill_4[i], 5];
}
console.log('Fill 5');
fileInstance.updateF0Points(update, true);
var fill_5 = fileInstance.getF0(null, true);
console.dir(fileInstance.getF0(null, true).slice(0,5));

for(var i = 0; i < base_f0.length; i++) {
    update[i + ''] = [fill_5[i], 6];
}
console.log('Fill 6');
fileInstance.updateF0Points(update, true);
var fill_6 = fileInstance.getF0(null, true);
console.dir(fileInstance.getF0(null, true).slice(0,5));

for(var i = 0; i < base_f0.length; i++) {
    update[i + ''] = [fill_6[i], 7];
}
console.log('Fill 7');
fileInstance.updateF0Points(update, true);
console.dir(fileInstance.getF0(null, true).slice(0,5));

console.log('Undo will fill 6', fileInstance.undo(null, true));
console.dir(fileInstance.getF0(null, true).slice(0,5));

console.log('Undo will fill 5', fileInstance.undo(null, true));
console.dir(fileInstance.getF0(null, true).slice(0,5));

console.log('Redo will fill 6', fileInstance.redo(null, true));
console.dir(fileInstance.getF0(null, true).slice(0,5));

console.log('Undo will fill 5', fileInstance.undo(null, true));
console.dir(fileInstance.getF0(null, true).slice(0,5));

console.log('Undo will fill 4', fileInstance.undo(null, true));
var n = fileInstance.getF0(null, true);
console.dir(n.slice(0,5));

for(var i = 0; i < base_f0.length; i++) {
    update[i + ''] = [n[i], 0];
}
console.log('Fill 0');
fileInstance.updateF0Points(update, true);
console.dir(fileInstance.getF0(null, true).slice(0,5));

console.log('Redo will be failed', fileInstance.redo(null, true));
console.dir(fileInstance.getF0(null, true).slice(0,5));

console.log('Undo will fill 4', fileInstance.undo(null, true));
console.dir(fileInstance.getF0(null, true).slice(0,5));

console.log('Undo will fill 3', fileInstance.undo(null, true));
console.dir(fileInstance.getF0(null, true).slice(0,5));

console.log('Undo will fill be failed', fileInstance.undo(null, true));
console.dir(fileInstance.getF0(null, true).slice(0,5));

// fileInstance.synthesis(null, true);
// console.log('File save: ', fileInstance.saveToFile({
//     fileName: '/Users/yamachu/ana_syn_update.wav'
// }, true));
process.exit();
