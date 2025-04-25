export type CoveredCommitsColumn = Array<
    | [string, string, string, string]
    | {
          AuthorEmail: string;
          AuthorName: string;
          MessagePreview: string;
          CommitSha: string;
      }
>;
