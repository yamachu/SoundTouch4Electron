// Overview of edge.js: http://tjanczuk.github.com/edge
// import electron-edge: require node module 50
console.dir(process.versions);
var edge = require('electron-edge');

var hello = edge.func('async (input) => { return ".NET welcomes " + input.ToString(); }');

hello('Node.js, electron-edge enabled!', function (error, result) {
	if (error) throw error;
	console.log(result);

	process.exit();
});