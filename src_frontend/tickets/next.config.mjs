/** @type {import('next').NextConfig} */
const nextConfig = {
  basePath: '',
  async redirects() {
    return [
      {
        source: '/',
        destination: '/tickets-list',
        permanent: true,
      },
    ]
  },
}

export default nextConfig