import type { Preview } from "@storybook/react";
import * as React from "react";
import { MemoryRouter } from "react-router-dom";
import "../src/CommonStyles/BaseTypography.css";
import "../src/CommonStyles/Fonts.css";
import { GlobalStyle } from "../src/CommonStyles/GlobalStyle";
import "../src/CommonStyles/Reset.css";
import { TestAnalyticsThemeProvider } from "../src/Theme/TestAnalyticsThemeProvider";
import { ForcedDarkModeContext } from "../src/Theme/UseTestAnalyticsDarkMode";

const preview: Preview = {
    decorators: [
        (Story, context) => {
            return (
                <ForcedDarkModeContext.Provider value={context.globals.theme == "dark"}>
                    <MemoryRouter>
                        <TestAnalyticsThemeProvider>
                            <GlobalStyle />
                            <Story />
                        </TestAnalyticsThemeProvider>
                    </MemoryRouter>
                </ForcedDarkModeContext.Provider>
            );
        },
    ],

    globalTypes: {
        theme: {
            description: "Global theme for components",
            toolbar: {
                title: "Theme",
                icon: "circlehollow",
                items: ["light", "dark"],
                dynamicTitle: true,
            },
        },
    },
    parameters: {
        controls: {
            matchers: {
                color: /(background|color)$/i,
                date: /Date$/i,
            },
        },
    },
};

export default preview;
