/* eslint-disable */
const { resolve } = require("path");
const HtmlWebpackPlugin = require("html-webpack-plugin");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");

module.exports = (env, argv) => {
    const isProduction = argv.mode === "production";
    const analyzeBundle = env && env.analyze;

    return {
        mode: isProduction ? "production" : "development",
        entry: "./src/index.tsx",
        output: {
            path: resolve(__dirname, "dist"),
            filename: isProduction ? "static/index.[contenthash].js" : "static/index.js",
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
                    exclude: /\.module\.css$/i,
                    use: [isProduction ? MiniCssExtractPlugin.loader : "style-loader", "css-loader"],
                },
                {
                    test: /\.module\.css$/i,
                    use: [
                        isProduction ? MiniCssExtractPlugin.loader : "style-loader",
                        {
                            loader: "css-loader",
                            options: {
                                modules: {
                                    namedExport: false,
                                    localIdentName: isProduction ? "[hash:base64:6]" : "[name]-[local]-[hash:base64:2]",
                                },
                                // importLoaders: 1,
                            },
                        },
                    ],
                },
            ],
        },
        plugins: [
            new HtmlWebpackPlugin({ inject: "body", template: "./src/index.html", filename: "index.html" }),
            (function () {
                if (isProduction) return null;
                const ReactRefreshWebpackPlugin = require("@pmmmwh/react-refresh-webpack-plugin");
                return new ReactRefreshWebpackPlugin();
            })(),
            (function () {
                if (!isProduction) return null;
                return new MiniCssExtractPlugin({
                    filename: "static/[name].[contenthash].css"
                });
            })(),
            (function () {
                if (!analyzeBundle) return null;
                const { BundleAnalyzerPlugin } = require("webpack-bundle-analyzer");
                return new BundleAnalyzerPlugin();
            })(),
        ].filter(Boolean),
        devServer: {
            historyApiFallback: {
                index: "/",
            },
            hot: true,
            client: {
                overlay: true,
                progress: true,
            },
            proxy: [
                {
                    context: ["/api/"],
                    target: "http://localhost:8124",
                },
            ],
        },
    };
};
