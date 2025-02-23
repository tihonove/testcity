/* eslint-disable */
module.exports = {
    reporters: [
        "default",
        ...(process.env.GITLAB_CI
            ? [["jest-junit", { outputDirectory: "./.test-reports", outputName: "junit.xml" }]]
            : []),
    ],
};
