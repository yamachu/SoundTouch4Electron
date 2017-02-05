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

var base_f0 = fileInstance.getF0(null, true);
var update = {};
for(var i = 0; i < base_f0.length; i++) {
    update[i + ''] = 150;
}
fileInstance.updateF0Points(update, true);

fileInstance.synthesis(null, true);
console.log('File save: ', fileInstance.saveToFile({
    fileName: '/Users/yamachu/ana_syn_update.wav'
}, true));
process.exit();
