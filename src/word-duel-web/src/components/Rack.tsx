import { pointsFor } from '../board/letterValues'

interface RackProps {
  rack: string
  usedLetters?: string
}

/** Renders the player's own rack, dimming tiles already spent in the current move preview. */
export function Rack({ rack, usedLetters = '' }: RackProps) {
  const remaining = [...usedLetters]
  const tiles = [...rack].map((letter) => {
    const usedIndex = remaining.indexOf(letter)
    const isUsed = usedIndex !== -1
    if (isUsed) {
      remaining.splice(usedIndex, 1)
    }
    return { letter, isUsed }
  })

  return (
    <div className="rack" aria-label="Your rack">
      {tiles.length === 0 && <p className="rack-empty">No tiles left.</p>}
      {tiles.map(({ letter, isUsed }, i) => (
        <div key={`${letter}-${i}`} className={`rack-tile${isUsed ? ' spent' : ''}`}>
          <span className="letter">{letter}</span>
          <span className="points">{pointsFor(letter)}</span>
        </div>
      ))}
    </div>
  )
}
