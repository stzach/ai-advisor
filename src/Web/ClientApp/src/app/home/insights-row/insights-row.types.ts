export type InsightCategory = 'earn' | 'optimise' | 'review';

export interface DashboardInsight {
  id:       string;
  category: InsightCategory;
  icon:     string;
  tag:      string;
  message:  string;
  cta:      string;
}

export interface DashboardInsightsData {
  insights: DashboardInsight[];
}
