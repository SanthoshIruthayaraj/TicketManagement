export interface TicketRow {
  TicketId: number;
  PublicTicketId: string;
  Title: string;
  Status: string;
  Priority: string;
  Category: string;
  Department: string;
  CreatedBy: string;
  Assignee: string;
  DueDate: Date;
  ResponseDue: Date | null;
  UpdatedAt: Date;
  CreatedAt: Date;
}
