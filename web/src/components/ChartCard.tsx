import type { ReactNode } from 'react'

interface Props {
  title: string
  /** One sentence for screen readers about what the chart shows. */
  summary: string
  controls?: ReactNode
  /** Column headings and rows for the table view under the chart. */
  table: { columns: string[]; rows: (string | number)[][] }
  children: ReactNode
}

/** A chart with its title, controls and a table of the same numbers for anyone who cannot use the chart. */
export default function ChartCard({ title, summary, controls, table, children }: Props) {
  return (
    <section className="chart-card section">
      <div className="chart-head">
        <h2>{title}</h2>
        {controls}
      </div>
      <div className="chart" role="img" aria-label={summary}>
        {children}
      </div>
      <details className="small" style={{ margin: '8px 8px 0' }}>
        <summary>Show as table</summary>
        <table className="data-table">
          <thead>
            <tr>
              {table.columns.map((c) => (
                <th key={c} scope="col">
                  {c}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {table.rows.map((row, i) => (
              <tr key={i}>
                {row.map((cell, j) => (
                  <td key={j}>{cell}</td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </details>
    </section>
  )
}
