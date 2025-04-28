import eslint from "@eslint/js";
import tseslint from "typescript-eslint";
import eslintConfigPrettier from "eslint-config-prettier";
import eslintPluginPrettierRecommended from "eslint-plugin-prettier/recommended";

export default [
    ...tseslint.config(
        eslint.configs.recommended,
        tseslint.configs.strictTypeChecked,
        eslintPluginPrettierRecommended,
        eslintConfigPrettier,
        {
            languageOptions: {
                parserOptions: {
                    projectService: true,
                    tsconfigRootDir: import.meta.dirname,
                },
            },
            rules: {
                "@typescript-eslint/no-unused-vars": "off",
            },
        },
        {
            files: ["*.js", "*.mjs"],
            extends: [tseslint.configs.disableTypeChecked],
        }
    ),
    {
        ignores: ["nginx-clickhouse-proxy/*", "dist/*"],
    },
];
