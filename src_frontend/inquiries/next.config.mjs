/** @type {import('next').NextConfig} */
const nextConfig = {
  basePath: '',
  async redirects() {
    return [
      {
        source: '/',
        destination: '/inquiries-list',
        permanent: true,
      },
    ]
  },
}

export default nextConfig