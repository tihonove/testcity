/* eslint-disable */
module.exports = (env, argv) => {
    const config = require("./webpack.config")(env, argv);
    return ({
        ...config,
        devServer: {
            ...config.devServer,
            proxy: config.devServer.proxy.map(x => ({
                ...x,
                target: "https://testcity.kube.testkontur.ru",
                secure: false,
                changeOrigin: true,
            })),
        },
    });
};
