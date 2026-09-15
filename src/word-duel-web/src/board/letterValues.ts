// Mirrors WordDuel.Domain/Tiles/LetterValues.cs — display only.
const POINTS: Record<string, number> = {
  A: 1, B: 4, C: 4, D: 2, E: 1, F: 4, G: 3, H: 4, I: 1, J: 9,
  K: 6, L: 2, M: 4, N: 2, O: 1, P: 4, Q: 9, R: 1, S: 1, T: 1,
  U: 2, V: 5, W: 4, X: 7, Y: 4, Z: 9,
}

export function pointsFor(letter: string): number {
  return POINTS[letter.toUpperCase()] ?? 0
}
