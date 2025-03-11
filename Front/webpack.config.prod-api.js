/* eslint-disable */
const config = require("./webpack.config");

module.exports = {
    ...config,
    devServer: {
        ...config.devServer,
        proxy: config.devServer.proxy.map(x => ({ ...x, target: "http://singular/test-analytics" }))
    }
}
