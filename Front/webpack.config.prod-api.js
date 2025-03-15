/* eslint-disable */
const config = require("./webpack.config");

module.exports = {
    ...config,
    devServer: {
        ...config.devServer,
        proxy: config.devServer.proxy.map(x => ({ 
            ...x, 
            target: "https://testcity.kube.testkontur.ru", 
            secure: false,
            changeOrigin: true,
        })),
    }
}
