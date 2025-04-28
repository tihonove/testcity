/* eslint-disable no-undef */
module.exports = function (api) {
    api.cache(true);

    const presets = ["@babel/preset-env", "@babel/preset-typescript", "@babel/react"];
    const plugins = ["@babel/plugin-proposal-class-properties"];

    if (process.env.NODE_ENV !== "production" && process.env.NODE_ENV !== "test") {
        plugins.push("react-refresh/babel");
    }

    return {
        presets,
        plugins,
    };
};
