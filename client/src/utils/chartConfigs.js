/**
 * Chart configuration presets for TrendLineChart component
 * 
 * Each config defines how to render a specific metric chart.
 * Use these with <TrendLineChart :config="chartConfigs.winrate(props)" :data="data" />
 */

/**
 * Winrate Chart Configuration
 * @param {Object} options - { overallWinRate: number }
 */
export function winrateConfig(options = {}) {
  return {
    dataKey: 'winRate',
    label: 'Winrate %',
    color: (data) => {
      if (!data || data.length === 0) return '#6d28d9'
      const lastWinrate = data[data.length - 1]?.winRate ?? 50
      if (lastWinrate >= 52) return '#22c55e' // Green
      if (lastWinrate < 48) return '#ef4444' // Red
      return '#6d28d9' // Purple (neutral)
    },
    tooltip: {
      title: (point) => `Game ${point.gameIndex}`,
      label: (point) => [
        `Winrate: ${point.winRate.toFixed(1)}%`,
        `Record: ${point.wins}-${point.losses}`,
        `Game ${point.gameIndex}: ${point.isWin ? 'Win' : 'Loss'}`
      ]
    },
    yAxis: {
      min: 0,
      max: 100,
      formatter: (value) => `${value}%`
    },
    annotations: options.overallWinRate !== null && options.overallWinRate !== undefined
      ? [{
          value: options.overallWinRate,
          label: `Overall: ${options.overallWinRate.toFixed(1)}%`,
          color: 'rgba(255, 255, 255, 0.4)',
          labelPosition: 'end'
        }]
      : []
  }
}

/**
 * Deaths Chart Configuration
 * @param {Object} options - { overallAverage: number, trend: string }
 */
export function deathsConfig(options = {}) {
  return {
    dataKey: 'rollingAverage',
    label: 'Deaths',
    color: {
      type: 'trend',
      trend: options.trend || 'neutral'
    },
    tooltip: {
      title: (point) => `Game ${point.gameIndex} - ${point.championName}`,
      label: (point) => {
        const date = new Date(point.timestamp).toLocaleDateString('en-US', {
          month: 'short', day: 'numeric', year: 'numeric'
        })
        return [
          `Deaths: ${point.deaths}`,
          `Rolling Avg: ${point.rollingAverage.toFixed(2)}`,
          point.role ? `Role: ${point.role}` : null,
          `Date: ${date}`
        ].filter(line => line !== null)
      }
    },
    yAxis: {
      min: 0,
      formatter: (value) => value.toFixed(1)
    },
    annotations: options.overallAverage !== null && options.overallAverage !== undefined
      ? [{
          value: options.overallAverage,
          label: `Overall: ${options.overallAverage.toFixed(1)}`,
          color: 'rgba(255, 255, 255, 0.4)',
          labelPosition: 'end'
        }]
      : []
  }
}

/**
 * Dragon Participation Chart Configuration
 * @param {Object} options - { overallAverage: number, trend: string }
 */
export function dragonParticipationConfig(options = {}) {
  return {
    dataKey: 'rollingAverage',
    label: 'Dragon Participation',
    color: {
      type: 'trend',
      trend: options.trend || 'neutral'
    },
    tooltip: {
      title: (point) => `Game ${point.gameIndex} - ${point.championName}`,
      label: (point) => {
        const date = new Date(point.timestamp).toLocaleDateString('en-US', {
          month: 'short', day: 'numeric', year: 'numeric'
        })
        return [
          `Participation: ${point.participationRate.toFixed(1)}%`,
          `Rolling Avg: ${point.rollingAverage.toFixed(1)}%`,
          `Team Dragons: ${point.teamDragons}`,
          `Participated: ${point.dragonsParticipated}`,
          point.role ? `Role: ${point.role}` : null,
          `Date: ${date}`
        ].filter(line => line !== null)
      }
    },
    yAxis: {
      min: 0,
      max: 100,
      formatter: (value) => `${value}%`
    },
    annotations: [
      {
        value: 70,
        label: 'Target: 70%',
        color: 'rgba(34, 197, 94, 0.5)',
        labelBackground: 'rgba(34, 197, 94, 0.7)',
        labelPosition: 'start'
      },
      ...(options.overallAverage !== null && options.overallAverage !== undefined
        ? [{
            value: options.overallAverage,
            label: `Overall: ${options.overallAverage.toFixed(1)}%`,
            color: 'rgba(255, 255, 255, 0.4)',
            labelPosition: 'end'
          }]
        : [])
    ]
  }
}

/**
 * Vision Score Chart Configuration
 * @param {Object} options - { overallAverage: number, roleTarget: number, trend: string }
 */
export function visionScoreConfig(options = {}) {
  const roleTarget = options.roleTarget || 1.0
  const targetLabel = roleTarget >= 2.0 ? 'Support Target: 2.0/min' : 'Target: 1.0/min'
  
  return {
    dataKey: 'rollingAverage',
    label: 'Vision Score',
    color: (data) => {
      if (!data || data.length === 0) return '#6d28d9'
      
      // Calculate average vision per minute from recent games
      const recentCount = Math.min(10, data.length)
      const recentGames = data.slice(-recentCount)
      const recentAvg = recentGames.reduce((sum, point) => sum + point.visionScorePerMinute, 0) / recentCount
      
      // Color based on performance relative to target
      if (recentAvg >= roleTarget) return '#22c55e' // Green - meeting target
      if (recentAvg >= roleTarget * 0.8) return '#eab308' // Yellow - approaching target
      return '#ef4444' // Red - below target
    },
    tooltip: {
      title: (point) => `Game ${point.gameIndex} - ${point.championName}`,
      label: (point) => {
        const date = new Date(point.timestamp).toLocaleDateString('en-US', {
          month: 'short', day: 'numeric', year: 'numeric'
        })
        return [
          `Vision/Min: ${point.visionScorePerMinute.toFixed(2)}`,
          `Rolling Avg: ${point.rollingAverage.toFixed(2)}`,
          `Vision Score: ${point.visionScore}`,
          `Game Duration: ${point.gameDurationMinutes.toFixed(1)} min`,
          point.role ? `Role: ${point.role}` : null,
          `Date: ${date}`
        ].filter(line => line !== null)
      }
    },
    yAxis: {
      min: 0,
      suggestedMax: Math.max(roleTarget * 1.5, 3.0),
      formatter: (value) => value.toFixed(1)
    },
    annotations: [
      {
        value: roleTarget,
        label: targetLabel,
        color: 'rgba(34, 197, 94, 0.5)',
        labelBackground: 'rgba(34, 197, 94, 0.7)',
        labelPosition: 'start'
      },
      ...(options.overallAverage !== null && options.overallAverage !== undefined
        ? [{
            value: options.overallAverage,
            label: `Overall: ${options.overallAverage.toFixed(2)}`,
            color: 'rgba(255, 255, 255, 0.4)',
            labelPosition: 'end'
          }]
        : [])
    ]
  }
}

/**
 * Gold at 15 Chart Configuration
 */
export function goldAt15Config() {
  return {
    dataKey: 'playerGold',
    label: 'Your Gold',
    showLegend: true,
    color: (data) => {
      if (!data || data.length === 0) return '#6d28d9'
      
      // Calculate average gold differential
      const validDiffs = data.filter(p => p.goldDifferential !== null)
      if (validDiffs.length === 0) return '#6d28d9'
      
      const avgDiff = validDiffs.reduce((acc, p) => acc + p.goldDifferential, 0) / validDiffs.length
      
      if (avgDiff >= 0) return '#22c55e' // Green
      return '#ef4444' // Red
    },
    additionalDatasets: [
      {
        label: 'Opponent Gold',
        dataKey: 'opponentGold',
        borderColor: 'rgba(255, 255, 255, 0.4)',
        backgroundColor: 'transparent',
        borderDash: [5, 5]
      }
    ],
    tooltip: {
      title: (point) => `Game ${point.gameIndex}: ${point.championName}`,
      label: (point, context) => {
        // Show dataset-specific value only
        if (context.datasetIndex === 0) {
          return `Your Gold: ${point.playerGold?.toLocaleString() || '--'}`
        } else {
          return `Opponent: ${point.opponentGold?.toLocaleString() || '--'}`
        }
      },
      footer: (point) => {
        const footerLines = []
        
        if (point.opponentGold !== null) {
          const diff = point.goldDifferential
          const sign = diff >= 0 ? '+' : ''
          footerLines.push(`Differential: ${sign}${diff?.toLocaleString() || '--'}`)
        }
        if (point.role) {
          footerLines.push(`Role: ${point.role}`)
        }
        const date = new Date(point.timestamp).toLocaleDateString('en-US', {
          month: 'short', day: 'numeric', year: 'numeric'
        })
        footerLines.push(`Date: ${date}`)
        
        return footerLines
      }
    },
    yAxis: {
      formatter: (value) => `${(value / 1000).toFixed(1)}k`
    },
    annotations: []
  }
}

/**
 * CS Per Minute Chart Configuration
 * @param {Object} options - { roleTarget: number }
 */
export function csPerMinuteConfig(options = {}) {
  return {
    dataKey: 'csPerMinute',
    label: 'CS/min',
    color: (data) => {
      if (!data || data.length === 0) return '#6d28d9'
      const sum = data.reduce((acc, p) => acc + p.csPerMinute, 0)
      const avg = sum / data.length
      
      // Good: >= 6 CS/min, Needs work: < 5 CS/min
      if (avg >= 6) return '#22c55e' // Green
      if (avg < 5) return '#ef4444' // Red
      return '#6d28d9' // Purple (neutral)
    },
    tooltip: {
      title: (point) => `Game ${point.gameIndex} - ${point.championName}`,
      label: (point) => {
        const date = new Date(point.timestamp).toLocaleDateString('en-US', {
          month: 'short', day: 'numeric', year: 'numeric'
        })
        return [
          `CS/Min: ${point.csPerMinute.toFixed(2)}`,
          `Total CS: ${point.totalCs}`,
          `Game Duration: ${point.gameDurationMinutes.toFixed(1)} min`,
          point.role ? `Role: ${point.role}` : null,
          `Date: ${date}`
        ].filter(line => line !== null)
      }
    },
    yAxis: {
      min: 0,
      formatter: (value) => value.toFixed(1)
    },
    annotations: options.roleTarget !== null && options.roleTarget !== undefined
      ? [{
          value: options.roleTarget,
          label: `Target: ${options.roleTarget.toFixed(1)} CS/min`,
          color: 'rgba(255, 255, 255, 0.4)',
          labelPosition: 'end'
        }]
      : []
  }
}
