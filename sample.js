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
    fileName: '/Users/yamachu/hoge.wav'
}, true));

fileInstance.synthesis(null, true);
console.log('Finish Synthesis');
console.log('File save: ', fileInstance.saveToFile({
    fileName: '/Users/yamachu/hoge.wav'
}, true));

var update = {};
for(var i = 0; i < fileInstance.getF0Length(null, true) - 2; i++) {
    update[i + ''] = 150;
}
fileInstance.updateF0Points(update, true);
fileInstance.synthesis(null, true);
console.log('File save: ', fileInstance.saveToFile({
    fileName: '/Users/yamachu/hoge_update.wav'
}, true));
process.exit();
