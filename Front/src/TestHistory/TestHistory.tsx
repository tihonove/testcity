import * as React from "react";
import styled from "styled-components";
import {ColumnStack, Fill, Fit, RowStack} from "@skbkontur/react-stack-layout";
import {Checkbox, ComboBox, Select} from "@skbkontur/react-ui";
import {MediaUiAPlayIcon, ShareNetworkIcon} from "@skbkontur/icons";

const Title = styled.h1({
    fontSize: "32px",
    lineHeight: "40px",
})

const TestRunsTable = styled.table({
    width: "100%",
    tableLayout: "fixed",
})

interface TestHistoryProps {
    runStatistics: Array<[state: string, duration: number, startDate: string]>;
}

export function TestHistory(props: TestHistoryProps): React.JSX.Element {

    return <ColumnStack gap={4} block stretch>
        <Fit>
            <Title>Test History: PackagePatcherDIModuleTests.KernelGet_EchelonExecutor_IsNotNull</Title>
            <div>Test History: PackagePatcherDIModuleTests.KernelGet_EchelonExecutor_IsNotNull</div>
        </Fit>
        <Fit>
            <RowStack gap={2}>
                <Fit>
                    <ComboBox
                        value={{value: "master"}}
                        getItems={async () => [{value: "master"}, {value: "tihonove/zzz"}]}
                        onValueChange={() => {

                        }}
                        renderValue={x => <span> <MediaUiAPlayIcon/> {x.value}</span>}
                        renderItem={x => <span> <MediaUiAPlayIcon/> {x.value}</span>}
                    />
                </Fit>
                <Fit>
                    <ComboBox
                        value={{value: "master"}}
                        getItems={async () => [{value: "master"}, {value: "tihonove/zzz"}]}
                        onValueChange={() => {

                        }}
                        renderValue={x => <span> <ShareNetworkIcon/> {x.value}</span>}
                        renderItem={x => <span> <ShareNetworkIcon/> {x.value}</span>}
                    />
                </Fit>
            </RowStack>
        </Fit>
        <Fit>
            Stats
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
                <tr>
                    <th>Status</th>
                    <th>Duration</th>
                    <th>Build</th>
                    <th>Agent</th>
                    <th>Started</th>
                </tr>
                <tr>
                    <th>

                    </th>
                    <th>Duration</th>
                    <th>Build</th>
                    <th>Agent</th>
                    <th>Started</th>
                </tr>
            </TestRunsTable>
        </Fit>
    </ColumnStack>;
}