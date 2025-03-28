import { styled } from "styled-components";

export const RunsTable = {
    columnCount: 6,
} as const;

export const ProjectsWithRunsTable = styled.table`
    width: 100%;

    td {
        padding: 6px 8px;
    }

    thead > tr > th {
        padding-top: 16px;
    }
`;
