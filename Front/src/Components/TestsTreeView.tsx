/* eslint-disable @typescript-eslint/no-non-null-assertion */
import { CheckCircleIcon16Regular } from "@skbkontur/icons/CheckCircleIcon16Regular";
import { ShapeCircleMOffIcon16Regular } from "@skbkontur/icons/ShapeCircleMOffIcon16Regular";
import { TimeClockIcon16Regular } from "@skbkontur/icons/TimeClockIcon16Regular";
import { XCircleIcon16Regular } from "@skbkontur/icons/XCircleIcon16Regular";
import React, { useMemo } from "react";
import { useParams } from "react-router-dom";
import styles from "./TestsTreeView.module.css";
import { ApproximateUnits, ITEMS_UNITS, TIME_UNITS } from "./ApproximateUnits";
import { TreeNode, TreeView } from "./TreeView";
import { RunStatus } from "./RunStatus";
import { useTestCityRequest } from "../Domain/Api/TestCityApiClient";
import { useProjectContextFromUrlParams } from "./useProjectContextFromUrlParams";

interface TestInfo {
    testId: string;
    duration: number;
    state: RunStatus;
}

type TestStats = { duration: number } & Record<RunStatus, number>;

const initialStats: TestStats = { duration: 0, Failed: 0, Skipped: 0, Success: 0 };

export function TestsTreeView(): React.JSX.Element {
    const { pathToGroup } = useProjectContextFromUrlParams();
    const { jobId = "", jobRunId = "" } = useParams();
    const [prefix, onChangePrefix] = React.useState<string | undefined>(undefined);

    const allTestsRaw = useTestCityRequest(
        c => c.runs.getTestList(pathToGroup, jobId, jobRunId, { itemsPerPage: 10000 }),
        [pathToGroup, jobId, jobRunId]
    );
    const allTests1 = useMemo(
        () => allTestsRaw.map(x => [x.finalState, x.testId, x.maxDuration] as const),
        [allTestsRaw]
    );

    const allTests = useMemo(
        () =>
            allTests1.map(x => ({
                testId: x[1].split(": ").slice(1).join(": "),
                duration: x[2],
                state: x[0],
            })),
        [allTests1]
    );

    const tree = useMemo(() => toTree(allTests), [allTests]);

    return (
        <TreeView
            data={tree}
            selectedPath={prefix}
            onSelect={n => {
                onChangePrefix(n.path);
            }}
            renderDetails={details => (
                <div style={{ whiteSpace: "nowrap" }}>
                    <span className={styles.countSuccess}>
                        {details.Success !== 0 && (
                            <>
                                <CheckCircleIcon16Regular />
                                <ApproximateUnits value={details.Success} units={ITEMS_UNITS} />
                            </>
                        )}
                    </span>{" "}
                    <span className={styles.countFailed}>
                        {details.Failed !== 0 && (
                            <>
                                <XCircleIcon16Regular />
                                <ApproximateUnits value={details.Failed} units={ITEMS_UNITS} />
                            </>
                        )}
                    </span>{" "}
                    <span className={styles.countSkipped}>
                        {details.Skipped !== 0 && (
                            <>
                                <ShapeCircleMOffIcon16Regular />
                                <ApproximateUnits value={details.Skipped} units={ITEMS_UNITS} />
                            </>
                        )}
                    </span>{" "}
                    {
                        <>
                            <TimeClockIcon16Regular />{" "}
                            {<ApproximateUnits value={details.duration} units={TIME_UNITS} />}
                        </>
                    }
                </div>
            )}
        />
    );
}

function splitTestName(testId: string): string[] {
    const regex =
        // eslint-disable-next-line no-useless-backreference
        /(?:(["'])(\\.|(?!\1)[^\\])*\1|\[(?:(["'])(\\.|(?!\2)[^\\])*\2|[^\]])*\]|\((?:(["'])(\\.|(?!\3)[^\\])*\3|[^)])*\)|[^.:])+/g;
    const parts = testId.match(regex);
    return [...(parts ?? []).map(x => x.trim())];
}

function toTree(tests: TestInfo[]): TreeNode<TestStats>[] {
    const root: TreeNode<TestStats>[] = [];

    tests.forEach(test => {
        const path = test.testId;
        const parts = splitTestName(path);
        let currentNodes = root;

        for (let i = 0; i < parts.length; i++) {
            const part = parts[i];
            const isFile = i === parts.length - 1;
            const type = isFile ? "file" : "directory";
            const existingNode = currentNodes.find(n => n.name === part && n.type === type);

            if (!existingNode) {
                const newNode: TreeNode<TestStats> = {
                    id: parts.slice(0, i + 1).join("/"),
                    name: part,
                    type,
                    path: parts.slice(0, i + 1).join("/"),
                    children: type === "directory" ? [] : undefined,
                    details: { ...initialStats },
                };
                newNode.details![test.state] += 1;
                newNode.details!.duration += test.duration;
                currentNodes.push(newNode);
                currentNodes = newNode.children!;
            } else {
                existingNode.details![test.state] += 1;
                existingNode.details!.duration += test.duration;
                currentNodes = existingNode.children!;
            }
        }
    });

    return mergeSingleChildDirectories(root);
}

function mergeSingleChildDirectories(nodes: TreeNode<TestStats>[]): TreeNode<TestStats>[] {
    for (let i = 0; i < nodes.length; i++) {
        nodes.sort((a, b) => {
            // if (a.type === 'directory' && b.type === 'file') {
            //   return -1;
            // }
            // if (a.type === 'file' && b.type === 'directory') {
            //   return 1;
            // }

            return b.details!.duration - a.details!.duration;
        });

        const node = nodes[i];
        if (node.type === "directory" && node.children?.length === 1) {
            const child = node.children[0];
            if (child.type === "directory") {
                // Merge node with its single directory child
                node.name += "/" + child.name;
                node.path = child.path;
                node.id = child.id;
                node.children = child.children;
                // Re-check this node as it might now have a single child
                i--;
            }
        } else if (node.type === "directory" && node.children) {
            mergeSingleChildDirectories(node.children);
        }
    }
    return nodes;
}
