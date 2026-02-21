/**
 * Chart.js global registration
 * 
 * Registers all Chart.js components and plugins once at app startup.
 * Import this file in main.js to ensure registration happens before
 * any chart component is rendered.
 */
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  RadialLinearScale,
  PointElement,
  LineElement,
  RadarController,
  Title,
  Tooltip,
  Legend,
  Filler
} from 'chart.js'
import annotationPlugin from 'chartjs-plugin-annotation'

ChartJS.register(
  CategoryScale,
  LinearScale,
  RadialLinearScale,
  PointElement,
  LineElement,
  RadarController,
  Title,
  Tooltip,
  Legend,
  Filler,
  annotationPlugin
)
