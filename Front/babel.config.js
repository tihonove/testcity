/* eslint-disable no-undef */
module.exports = function (api) {
    api.cache(true);

    const presets = ["@babel/preset-env", "@babel/preset-typescript", "@babel/react"];
    const plugins = ["babel-plugin-styled-components", "@babel/plugin-proposal-class-properties"];

    const isDevelopment = process.env.NODE_ENV !== "production";
    if (isDevelopment) {
        plugins.push("react-refresh/babel");
    }

    return {
        presets,
        plugins,
    };
};
