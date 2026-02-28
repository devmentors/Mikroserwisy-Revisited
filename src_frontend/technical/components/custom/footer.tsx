import Image from "next/image";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { 
  faGithub, 
  faYoutube, 
  faDiscord, 
  faInstagram, 
  faTwitter, 
  faLinkedinIn 
} from '@fortawesome/free-brands-svg-icons';

export function Footer() {
  return (
    <footer className="border-t py-4 bg-black">
      <div className="container flex flex-col items-center gap-3 mx-auto">
        <div className="flex items-center gap-4">
          <a href="https://devmentors.io">
            <Image 
              src="/img/devmentors-logo.png" 
              alt="DevMentors Logo" 
              width={80}
              height={19}
              className="h-auto w-auto"
            />
          </a>
          <a href="https://github.com/devmentors" className="text-sm text-white hover:opacity-75">
            <FontAwesomeIcon icon={faGithub} />
          </a>
          <a href="https://www.youtube.com/@DevMentorsPL" className="text-sm text-white hover:opacity-75">
            <FontAwesomeIcon icon={faYoutube} />
          </a>
          <a href="https://devmentors.io/discordpl" className="text-sm text-white hover:opacity-75">
            <FontAwesomeIcon icon={faDiscord} />
          </a>
          <a href="https://www.instagram.com/devmentors_pl/" className="text-sm text-white hover:opacity-75">
            <FontAwesomeIcon icon={faInstagram} />
          </a>
          <a href="https://twitter.com/dev_mentors_pl" className="text-sm text-white hover:opacity-75">
            <FontAwesomeIcon icon={faTwitter} />
          </a>
          <a href="https://www.linkedin.com/company/devmentors-io/" className="text-sm text-white hover:opacity-75">
            <FontAwesomeIcon icon={faLinkedinIn} />
          </a>
        </div>
      </div>
    </footer>
  );
} 