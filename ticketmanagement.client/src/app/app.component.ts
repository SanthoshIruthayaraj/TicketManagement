import { Component } from "@angular/core";
import { CommonModule, DatePipe } from "@angular/common";
import {
  GridModule,
  FilterService,
  SortService,
  EditService,
  ToolbarService,
  VirtualScrollService,
  ToolbarItems,
  EditSettingsModel,
  FilterSettingsModel,
} from "@syncfusion/ej2-angular-grids";
import { TicketRow } from "./app.types";
import { DataManager, UrlAdaptor } from "@syncfusion/ej2-data";

const BASE_URL = "https://localhost:7000/api/tickets"; // your ASP.NET Core server API URL

@Component({
  selector: "app-root",
  standalone: true,
  imports: [CommonModule, DatePipe, GridModule],
  templateUrl: "app.component.html",
  styleUrls: ["app.component.css"],
  providers: [VirtualScrollService, FilterService, SortService, EditService, ToolbarService],
})
export class AppComponent {
  public dataManager: DataManager = new DataManager({
    url: `${BASE_URL}`,
    insertUrl: `${BASE_URL}/insert`,
    updateUrl: `${BASE_URL}/update`,
    removeUrl: `${BASE_URL}/remove`,
    adaptor: new UrlAdaptor(),
    crossDomain: true,
  });

  public toolbar: ToolbarItems[] = [
    "Add",
    "Edit",
    "Delete",
    "Update",
    "Cancel",
    "Search",
  ];

  public editSettings: EditSettingsModel = {
    allowEditing: true,
    allowAdding: true,
    allowDeleting: true,
    mode: "Normal",
  };

  public filterSettings: FilterSettingsModel = { type: "Excel" };

  getStatusClass(row: TicketRow): string {
    const map: Record<string, string> = {
      Open: "status-open",
      Closed: "status-closed",
      Pending: "status-pending",
    };
    return map[row.Status] ?? "";
  }

  getStatusDescription(row: TicketRow): string {
    return `Status: ${row.Status}`;
  }

  getPriorityClass(row: TicketRow): string {
    const map: Record<string, string> = {
      High: "priority-high",
      Medium: "priority-medium",
      Low: "priority-low",
    };
    return map[row.Priority] ?? "";
  }

  getPriorityDescription(row: TicketRow): string {
    return `Priority: ${row.Priority}`;
  }

  getCategoryClass(row: TicketRow): string {
    const map: Record<string, string> = {
      Bug: "chip-bug",
      Feature: "chip-feature",
      Task: "chip-task",
    };
    return map[row.Category] ?? "";
  }
}
