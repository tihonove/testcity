/* eslint-disable */
const { resolve } = require("path");
const HtmlWebpackPlugin = require("html-webpack-plugin");

module.exports = {
    mode: "development",
    entry: "./src/index.tsx",
    output: {
        path: resolve(__dirname, "dist"),
        filename: "static/index.js",
        publicPath: "/",
    },
    resolve: {
        extensions: [".ts", ".tsx", ".js", ".json"],
    },
    module: {
        rules: [
            {
                test: /\.(ts|js)x?$/,
                exclude: /node_modules/,
                loader: "babel-loader",
            },
            {
                test: /\.css$/i,
                use: ["style-loader", "css-loader"],
            },
        ],
    },
    plugins: [new HtmlWebpackPlugin({ inject: "body", template: "./src/index.html", filename: "index.html", })],
    devServer: {
        historyApiFallback: {
            index: "/",
        },
        proxy: [
            {
                context: ["/clickhouse/"],
                target: "http://localhost:8124/test-analytics",
            },
            {
                context: ["/gitlab/"],
                target: "http://localhost:8124/test-analytics",
            },
        ],
    },
};
