// Mirrors the DTOs of the FitTrack API. Dates are ISO strings: timestamps in UTC
// ("2026-10-12T17:30:00Z"), calendar dates as "2026-10-12".

export type Visibility = 'Private' | 'Friends'
export type ExerciseKind = 'Strength' | 'Cardio'
export type MealType = 'Breakfast' | 'Lunch' | 'Dinner' | 'Snack'
export type GoalMetric = 'BodyWeight' | 'WorkoutsPerWeek' | 'DailyCalories' | 'DailyProtein'
export type GoalStatus = 'Active' | 'Achieved' | 'Archived'
export type PostType = 'WorkoutLogged' | 'GoalAchieved' | 'WeeklyTargetMet'

export interface Me {
  id: string
  email: string
  displayName: string
  timeZone: string
  defaultVisibility: Visibility
  createdAt: string
}

export interface AuthResponse {
  accessToken: string
  expiresAt: string
  user: Me
}

export interface Page<T> {
  items: T[]
  nextBefore: string | null
}

export interface SetDto {
  position: number
  reps: number
  weightKg: number
}

export interface ExerciseDto {
  id: string
  position: number
  name: string
  kind: ExerciseKind
  durationSeconds: number | null
  distanceKm: number | null
  sets: SetDto[]
}

export interface Workout {
  id: string
  userId: string
  name: string
  performedAt: string
  durationMinutes: number
  notes: string | null
  visibility: Visibility
  createdAt: string
  exercises: ExerciseDto[]
}

export interface WorkoutRequest {
  name: string
  performedAt: string
  durationMinutes: number
  notes: string | null
  visibility: Visibility
  exercises: {
    name: string
    kind: ExerciseKind
    durationSeconds: number | null
    distanceKm: number | null
    sets: { reps: number; weightKg: number }[]
  }[]
}

export interface Meal {
  id: string
  userId: string
  name: string
  type: MealType
  eatenAt: string
  calories: number
  proteinG: number | null
  carbsG: number | null
  fatG: number | null
  notes: string | null
  visibility: Visibility
  createdAt: string
}

export interface DailyTotals {
  date: string
  mealCount: number
  calories: number
  proteinG: number
  carbsG: number
  fatG: number
}

export interface Measurement {
  id: string
  userId: string
  date: string
  weightKg: number
  bodyFatPct: number | null
  waistCm: number | null
  visibility: Visibility
  createdAt: string
}

export interface GoalProgress {
  current: number | null
  percent: number
  isMet: boolean
  period: 'latest' | 'thisWeek' | 'today'
}

export interface Goal {
  id: string
  userId: string
  metric: GoalMetric
  targetValue: number
  startValue: number | null
  startDate: string
  targetDate: string | null
  status: GoalStatus
  achievedAt: string | null
  visibility: Visibility
  progress: GoalProgress | null
}

export interface Friend {
  userId: string
  displayName: string
  since: string
}

export interface FriendRequest {
  id: string
  userId: string
  displayName: string
  direction: 'incoming' | 'outgoing'
  createdAt: string
}

export interface UserProfile {
  id: string
  displayName: string
  isSelf: boolean
  isFriend: boolean
  friendsSince: string | null
}

export interface Invite {
  id: string
  code: string
  createdAt: string
  expiresAt: string
  isUsed: boolean
  isExpired: boolean
  usedByName: string | null
  usedAt: string | null
}

export interface Post {
  id: string
  authorId: string
  authorName: string
  type: PostType
  summary: string
  workoutId: string | null
  goalId: string | null
  createdAt: string
}

export interface WeightTrend {
  points: { date: string; weightKg: number }[]
  targetKg: number | null
}

export interface WorkoutsPerWeek {
  weeks: { weekStart: string; workouts: number }[]
  target: number | null
}

export interface WeekVolume {
  weekStart: string
  volumeKg: number
}

export interface Nutrition {
  days: { date: string; calories: number | null; proteinG: number | null }[]
  caloriesTarget: number | null
  proteinTarget: number | null
}
