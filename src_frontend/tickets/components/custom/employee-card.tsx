import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Card, CardContent } from "@/components/ui/card"

interface EmployeeCardProps {
  name: string
  surname: string
  position: 'Supervisor' | 'Agent'
  avatarUrl?: string
}

export function EmployeeCard({ name, surname, position, avatarUrl }: EmployeeCardProps) {
  return (
    <Card className="w-[240px]">
      <CardContent className="flex items-center gap-2 p-3">
        <Avatar className="h-8 w-8">
          <AvatarImage src={avatarUrl} alt={`${name} ${surname}`} />
          <AvatarFallback className="text-xs">{`${name[0]}${surname[0]}`}</AvatarFallback>
        </Avatar>
        <div className="min-w-0">
          <p className="text-sm font-medium truncate">{`${name} ${surname}`}</p>
          <p className="text-xs text-muted-foreground">{position}</p>
        </div>
      </CardContent>
    </Card>
  )
} 