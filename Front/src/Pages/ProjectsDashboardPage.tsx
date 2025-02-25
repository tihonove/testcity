import { EyeClosedIcon16Regular, EyeOpenIcon16Regular, TextBulletIcon24Regular } from "@skbkontur/icons";
import { ColumnStack, Fit, RowStack } from "@skbkontur/react-stack-layout";
import { Button } from "@skbkontur/react-ui";
import * as React from "react";
import { Link, useParams } from "react-router-dom";
import styled from "styled-components";
import { useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { SubIcon } from "../Components/SubIcon";
import { JobIdWithParentProjectNames } from "../Domain/JobIdWithParentProject";
import { JobsView } from "../Domain/JobsView";
import { BranchSelect } from "../TestHistory/BranchSelect";
import { theme } from "../Theme/ITheme";
import { useSearchParamAsState } from "../Utils";
import { createLinkToCreateNewPipeline, createLinkToProject } from "./Navigation";
import { usePopularBranchStoring } from "../Utils/PopularBranchStoring";

export function ProjectsDashboardPage(): React.JSX.Element {
    const { groupIdLevel1, groupIdLevel2, groupIdLevel3 } = useParams();
    if (groupIdLevel1 == null || groupIdLevel1 === "") {
        throw new Error(`Group is not defined`);
    }

    const [currentBranchName, setCurrentBranchName] = useSearchParamAsState("branch");
    usePopularBranchStoring(currentBranchName);
    const [noRuns, setNoRuns] = useSearchParamAsState("noruns");

    const pathToGroup = [groupIdLevel1, groupIdLevel2, groupIdLevel3].filter(x => x != null);
    const rootProjectStructure = useStorageQuery(x => x.getRootProjectStructure(groupIdLevel1), [groupIdLevel1]);
    const projects = useStorageQuery(x => x.getProjects(pathToGroup), [pathToGroup]);
    const projectIds = React.useMemo(() => projects.map(p => p.id), [projects]);
    const allJobs = useStorageQuery(x => x.findAllJobs(projectIds), [projectIds]);
    const allJobRuns = useStorageQuery(
        x => x.findAllJobsRuns(projectIds, currentBranchName),
        [projectIds, currentBranchName]
    );

    return (
        <Root>
            <TestListRoot block gap={1}>
                <Fit>
                    <RowStack block gap={1} baseline>
                        <Header>Jobs</Header>
                    </RowStack>
                </Fit>
                <Fit>
                    <BranchSelect
                        branch={currentBranchName}
                        onChangeBranch={setCurrentBranchName}
                        projectIds={projectIds}
                    />
                </Fit>
                <Fit>
                    {projects.map(project => (
                        <React.Fragment key={project.id}>
                            <SectionTitle gap={5} baseline block>
                                <Fit>
                                    <Link
                                        className="no-underline"
                                        to={createLinkToProject(rootProjectStructure, project.id, currentBranchName)}>
                                        <Header3>{project.title}</Header3>
                                    </Link>
                                </Fit>
                                <Fit>
                                    <Button
                                        component="a"
                                        href={createLinkToCreateNewPipeline(
                                            rootProjectStructure,
                                            project.id,
                                            currentBranchName
                                        )}
                                        target="_blank">
                                        New pipeline
                                    </Button>
                                </Fit>
                            </SectionTitle>
                            <JobsView
                                hideRuns={noRuns === "1"}
                                currentBranchName={currentBranchName}
                                rootProjectStructure={rootProjectStructure}
                                allJobs={allJobs.filter(j => j[JobIdWithParentProjectNames.ProjectId] === project.id)}
                                allJobRuns={allJobRuns}
                            />
                        </React.Fragment>
                    ))}
                </Fit>
            </TestListRoot>
            <ShowRunsSwitchContainer>
                <Button
                    use="link"
                    onClick={() => {
                        setNoRuns(noRuns === "1" ? undefined : "1");
                    }}>
                    <SubIcon sub={noRuns ? <EyeClosedIcon16Regular /> : <EyeOpenIcon16Regular />}>
                        <TextBulletIcon24Regular />
                    </SubIcon>
                </Button>
            </ShowRunsSwitchContainer>
        </Root>
    );
}

const TestListRoot = styled(ColumnStack)`
    max-width: ${theme.layout.centered.width};
    width: ${theme.layout.centered.width};
    margin: 24px auto;
`;

const SectionTitle = styled(RowStack)`
    margin-top: 16px;
`;

const Root = styled.main`
    display: flex;
`;

const Header = styled.h1`
    font-size: 32px;
    line-height: 40px;
`;

const ShowRunsSwitchContainer = styled.div`
    position: fixed;
    right: 50px;
    top: 20px;
`;

const Header3 = styled.h3`
    font-size: 22px;
    line-height: 24px;
`;
