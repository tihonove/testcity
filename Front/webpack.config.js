const {resolve} = require("path");
const HtmlWebpackPlugin = require('html-webpack-plugin');

module.exports = {
    mode: "development",
    entry: './src/index.tsx',
    output: {
        path: resolve(__dirname, 'nginx-clickhouse-proxy', 'content'),
        filename: 'index.js',
        publicPath: "/test-analytics"
    },
    resolve: {
        extensions: ['.ts', '.tsx', '.js', '.json']
    },
    module: {
        rules: [{
            test: /\.(ts|js)x?$/,
            exclude: /node_modules/,
            loader: 'babel-loader',
        },
        {
            test: /\.css$/i,
            use: ["style-loader", "css-loader"],
        }],

    },
    plugins: [
        new HtmlWebpackPlugin({inject: "body", template: "./src/index.html"}),
        new HtmlWebpackPlugin({inject: "body", template: "./src/index.html", filename: "history/index.html"})
    ],
    devServer: {
        proxy: {
            '/test-analytics/clickhouse/': {
                target: 'http://vm-ch2-stg.dev.kontur.ru:8123',
                pathRewrite: { '^/test-analytics/clickhouse': '' },
            },
        },
    },
};