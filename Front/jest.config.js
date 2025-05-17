/* eslint-disable */
module.exports = {
    reporters: [
        "default",
        ...((process.env.GITLAB_CI || process.env.GITHUB_ACTIONS)
            ? [["jest-junit", { outputDirectory: "./.test-reports", outputName: "junit.xml" }]]
            : []),
    ],
};
