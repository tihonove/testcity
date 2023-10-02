import * as React from "react";
import styled from "styled-components";
import {ColumnStack, Fill, Fit, RowStack} from "@skbkontur/react-stack-layout";
import {Checkbox, ComboBox, MenuSeparator, Paging} from "@skbkontur/react-ui";
import {LogoMicrosoftIcon, MediaUiAPlayIcon, QuestionCircleIcon, ShareNetworkIcon} from "@skbkontur/icons";
import {RunStatisticsChart} from "../RunStatisticsChart/RunStatisticsChart";
import {formatDuration} from "../RunStatisticsChart/DurationUtils";

interface TestHistoryProps {
    testId: string;
    jobId: undefined | string;
    onChangeJobId: (value: undefined | string) => void;
    jobIds: string[];

    branch: undefined | string;
    onChangeBranch: (value: undefined | string) => void;
    branchNames: string[];

    stats: Array<[state: string, duration: number, startDate: string]>;

    totalRunCount: number,
    runsPage: number,
    onRunsPageChange: (nextPage: number) => void,
    runs: Array<[
        jobId: string,
        jobRunId: string,
        branchName: string,
        state: string,
        duration: number,
        startDateTime: string,
        agentName: string,
        agentOSName: string
    ]>;
}

function splitTestId(testId: string): [string, string] {
    const testIdParts = testId.match(/(?:[^\."]+|"[^"]*")+/g)
    if (testIdParts == null || testIdParts.length < 3) {
        return ["", testId];
    }
    return [testIdParts.slice(0, -2).join("."), testIdParts.slice(-2).join(".")];
}

export function TestHistory(props: TestHistoryProps): React.JSX.Element {
    const [suiteId, testId] = splitTestId(props.testId);

    return <ColumnStack gap={4} block stretch>
        <Fit>
            <Title>Test History: {testId}</Title>
            <div>{suiteId}</div>
        </Fit>
        <Fit>
            {props.stats.reduce((x, y) => y[0] == "Success" ? x + 1 : x, 0)} Success
            | {props.stats.reduce((x, y) => y[0] != "Success" ? x + 1 : x, 0)} Failed (of {props.stats.length} total)
        </Fit>
        <Fit>
            <RowStack gap={2}>
                <Fit>
                    <ComboBox
                        value={props.jobId}
                        getItems={async () => [undefined, <MenuSeparator/>, ...props.jobIds]}
                        onValueChange={x => {
                            if (typeof x === "string" || x == undefined)
                                props.onChangeJobId(x);
                        }}
                        itemToValue={x => (typeof x === "string") ? x : ""}
                        valueToString={x => typeof x === "string" ? x : ""}
                        placeholder={"All jobs"}
                        renderValue={x => x == undefined ? <span> <MediaUiAPlayIcon/> All jobs</span> :
                            <span> <MediaUiAPlayIcon/> {x}</span>}
                        renderItem={x => x == undefined ? <span> <MediaUiAPlayIcon/> All jobs</span> :
                            <span> <MediaUiAPlayIcon/> {x}</span>}
                    />
                </Fit>
                <Fit>
                    <ComboBox
                        value={props.branch}
                        getItems={async () => [undefined, <MenuSeparator/>, ...props.branchNames]}
                        onValueChange={x => {
                            if (typeof x === "string" || x == undefined)
                                props.onChangeBranch(x);
                        }}
                        itemToValue={x => (typeof x === "string") ? x : ""}
                        valueToString={x => typeof x === "string" ? x : ""}
                        placeholder={"All branches"}
                        renderValue={x => x == undefined ? <span> <ShareNetworkIcon/> All branches</span> :
                            <span> <ShareNetworkIcon/> {x}</span>}
                        renderItem={x => x == undefined ? <span> <ShareNetworkIcon/> All branches</span> :
                            <span> <ShareNetworkIcon/> {x}</span>}
                    />
                </Fit>
            </RowStack>
        </Fit>
        <Fit>
            <RunStatisticsChart value={props.stats}/>
        </Fit>
        <Fit>
            <RowStack block gap={2}>
                <Fill></Fill>
                <Fit>
                    <Checkbox>Average</Checkbox>
                </Fit>
                <Fit>
                    <Checkbox>Show failed</Checkbox>
                </Fit>
            </RowStack>
        </Fit>
        <Fit>
            <TestRunsTable>
                <thead>
                <TestRunsTableHeadRow>
                    <th>Status</th>
                    <th>Duration</th>
                    <th>Branch</th>
                    <th>Build</th>
                    <th>Agent</th>
                    <th>Started</th>
                </TestRunsTableHeadRow>
                </thead>
                <tbody>
                {props.runs.map(x => (
                    <TestRunsTableRow key={x[0]+"-" + x[1]}>
                        <td>{x[3]}</td>
                        <td>{formatDuration(x[4], x[4])}</td>
                        <td><ShareNetworkIcon/> {x[2]}</td>
                        <td>{x[0]} #{x[1]}</td>
                        <td>
                            {x[7] == "Windows" ? <LogoMicrosoftIcon/> : <QuestionCircleIcon />} {x[6]}
                        </td>
                        <td>{x[5]}</td>
                    </TestRunsTableRow>
                ))}
                </tbody>
            </TestRunsTable>
            <Paging
                activePage={props.runsPage}
                onPageChange={props.onRunsPageChange}
                pagesCount={Math.ceil(props.totalRunCount / 50)}
            />
        </Fit>
    </ColumnStack>;
}

const Title = styled.h1({
    fontSize: "32px",
    lineHeight: "40px",
})

const TestRunsTableHeadRow = styled.tr({
    th: {
        fontSize: "12px",
        textAlign: "left",
        padding: "4px 8px",
    },

    borderBottom: "1px solid #eee",
});

const TestRunsTableRow = styled.tr({
    td: {
        textAlign: "left",
        padding: "6px 8px",
    },
});

const TestRunsTable = styled.table({
    width: "100%",
    tableLayout: "fixed",

})

