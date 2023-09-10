const {resolve} = require("path");
const HtmlWebpackPlugin = require('html-webpack-plugin');

module.exports = {
    mode: "development",
    entry: './src/index.js',
    output: {
        path: resolve(__dirname, 'dist'),
        filename: 'index.js',
        publicPath: "/test-analytics"
    },
    plugins: [new HtmlWebpackPlugin({ })],
    devServer: {
        allowedHosts: "all",
        proxy: {
            '/': 'http://172.17.0.2:8123/',
        },
    }
};