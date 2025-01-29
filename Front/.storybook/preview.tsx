import type { Preview } from "@storybook/react";
import * as React from "react";
import "../src/CommonStyles/BaseTypography.css";
import "../src/CommonStyles/Fonts.css";
import { GlobalStyle } from "../src/CommonStyles/GlobalStyle";
import "../src/CommonStyles/Reset.css";
import { TestAnalyticsThemeProvider } from "../src/Theme/TestAnalyticsThemeProvider";

const preview: Preview = {
    decorators: [
        Story => (
            <TestAnalyticsThemeProvider>
                <GlobalStyle />
                <Story />
            </TestAnalyticsThemeProvider>
        ),
    ],
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
