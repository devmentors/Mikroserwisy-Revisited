export interface Client {
  userId: string
  personToken: string
  name: string
  email: string
}

export const CLIENTS: Client[] = [
  {
    userId: '10000000-0000-0000-0000-000000000001',
    personToken: 'demo-client-jan',
    name: 'Jan Klient',
    email: 'jan.klient@example.com'
  },
  {
    userId: '10000000-0000-0000-0000-000000000002',
    personToken: 'demo-client-anna',
    name: 'Anna Nowak',
    email: 'anna.nowak@example.com'
  },
  {
    userId: '10000000-0000-0000-0000-000000000003',
    personToken: 'demo-client-piotr',
    name: 'Piotr Wiśniewski',
    email: 'piotr.wisniewski@example.com'
  }
]
