(() => {
    const navbar = document.getElementById("navbar");
    const mobileMenuBtn = document.getElementById("mobileMenuBtn");
    const mobileMenu = document.getElementById("mobileMenu");
    const scrollToTopBtn = document.getElementById("scrollToTop");

    const closeMobileMenu = () => {
        if (!mobileMenu || !mobileMenuBtn) return;

        mobileMenu.classList.remove("active");
        const spans = mobileMenuBtn.querySelectorAll("span");
        if (spans.length === 3) {
            spans[0].style.transform = "";
            spans[1].style.opacity = "";
            spans[2].style.transform = "";
        }
    };

    const openOrToggleMobileMenu = () => {
        if (!mobileMenu || !mobileMenuBtn) return;

        mobileMenu.classList.toggle("active");
        const spans = mobileMenuBtn.querySelectorAll("span");
        if (spans.length !== 3) return;

        const isActive = mobileMenu.classList.contains("active");
        spans[0].style.transform = isActive ? "rotate(45deg) translate(5px, 5px)" : "";
        spans[1].style.opacity = isActive ? "0" : "";
        spans[2].style.transform = isActive ? "rotate(-45deg) translate(7px, -6px)" : "";
    };

    const scrollToSection = (sectionId) => {
        const section = document.getElementById(sectionId);
        if (!section) return;

        const offset = 80;
        const elementPosition = section.getBoundingClientRect().top;
        const offsetPosition = elementPosition + window.pageYOffset - offset;
        window.scrollTo({ top: offsetPosition, behavior: "smooth" });
    };

    const scrollToTop = () => {
        window.scrollTo({ top: 0, behavior: "smooth" });
    };

    window.scrollToSection = scrollToSection;
    window.scrollToTop = scrollToTop;

    const initNavbarScrollState = () => {
        if (!navbar) return;
        navbar.classList.toggle("scrolled", window.scrollY > 50);
    };

    const initMobileMenu = () => {
        if (mobileMenuBtn) {
            mobileMenuBtn.addEventListener("click", openOrToggleMobileMenu);
        }

        document.querySelectorAll(".mobile-link").forEach((link) => {
            link.addEventListener("click", closeMobileMenu);
        });

        document.addEventListener("keydown", (e) => {
            if (e.key === "Escape" && mobileMenu?.classList.contains("active")) {
                closeMobileMenu();
            }
        });
    };

    const initAnchorSmoothScroll = () => {
        document.querySelectorAll('a[href^="#"]').forEach((anchor) => {
            anchor.addEventListener("click", function (e) {
                e.preventDefault();
                const targetId = this.getAttribute("href")?.substring(1);
                if (targetId) {
                    scrollToSection(targetId);
                }
            });
        });
    };

    const initParticles = () => {
        const particlesContainer = document.getElementById("particles");
        if (!particlesContainer) return;

        for (let i = 0; i < 20; i++) {
            const particle = document.createElement("div");
            particle.style.position = "absolute";
            particle.style.width = "8px";
            particle.style.height = "8px";
            particle.style.background = "rgba(27, 174, 190, 0.35)";
            particle.style.borderRadius = "50%";
            particle.style.left = `${Math.random() * 100}%`;
            particle.style.top = `${Math.random() * 100}%`;

            const duration = 3000 + Math.random() * 2000;
            const delay = Math.random() * 2000;

            particle.animate(
                [
                    { transform: "translateY(0px)", opacity: 0.2 },
                    { transform: "translateY(-30px)", opacity: 0.5 },
                    { transform: "translateY(0px)", opacity: 0.2 }
                ],
                {
                    duration,
                    delay,
                    iterations: Infinity,
                    easing: "ease-in-out"
                }
            );

            particlesContainer.appendChild(particle);
        }
    };

    const initSectionReveal = () => {
        const observer = new IntersectionObserver(
            (entries) => {
                entries.forEach((entry) => {
                    if (entry.isIntersecting) {
                        entry.target.style.opacity = "1";
                        entry.target.style.transform = "translateY(0)";
                    }
                });
            },
            { threshold: 0.1, rootMargin: "-50px" }
        );

        document.querySelectorAll("section").forEach((section) => {
            section.style.opacity = "0";
            section.style.transform = "translateY(30px)";
            section.style.transition = "opacity 0.6s ease, transform 0.6s ease";
            observer.observe(section);
        });
    };

    const initStatsCounter = () => {
        const statNumbers = document.querySelectorAll(".stat-number");
        if (!statNumbers.length) return;

        const animateCounter = (element) => {
            const target = parseInt(element.getAttribute("data-target") || "0", 10);
            const duration = 2000;
            const increment = target / (duration / 16);
            let current = 0;

            const updateCounter = () => {
                current += increment;
                if (current < target) {
                    element.textContent = Math.floor(current).toString();
                    requestAnimationFrame(updateCounter);
                } else {
                    element.textContent = target.toString();
                }
            };

            updateCounter();
        };

        const statsObserver = new IntersectionObserver(
            (entries) => {
                entries.forEach((entry) => {
                    if (entry.isIntersecting && !entry.target.classList.contains("animated")) {
                        entry.target.classList.add("animated");
                        animateCounter(entry.target);
                        statsObserver.unobserve(entry.target);
                    }
                });
            },
            { threshold: 0.5 }
        );

        statNumbers.forEach((stat) => statsObserver.observe(stat));
    };

    const initFaq = () => {
        const faqItems = document.querySelectorAll(".faq-item");
        faqItems.forEach((item) => {
            const question = item.querySelector(".faq-question");
            if (!question) return;

            question.addEventListener("click", () => {
                const isActive = item.classList.contains("active");
                faqItems.forEach((otherItem) => {
                    if (otherItem !== item) otherItem.classList.remove("active");
                });

                item.classList.toggle("active", !isActive);
            });
        });
    };

    const initScrollTopButton = () => {
        if (!scrollToTopBtn) return;

        window.addEventListener("scroll", () => {
            scrollToTopBtn.classList.toggle("visible", window.scrollY > 300);
        });

        scrollToTopBtn.addEventListener("click", scrollToTop);
    };

    const initParallax = () => {
        window.addEventListener("scroll", () => {
            const scrolled = window.scrollY;
            const heroContent = document.querySelector(".hero-content");
            if (heroContent && scrolled < 500) {
                heroContent.style.transform = `translateY(${scrolled * 0.3}px)`;
                heroContent.style.opacity = String(1 - scrolled / 300);
            }
        });
    };

    const initHeroLoadAnimations = () => {
        window.addEventListener("load", () => {
            const heroElements = document.querySelectorAll(".fade-in, .fade-in-up");
            heroElements.forEach((element, index) => {
                setTimeout(() => {
                    element.style.opacity = "1";
                    element.style.transform = "translateY(0)";
                }, index * 100);
            });
        });
    };

    initNavbarScrollState();
    initMobileMenu();
    initAnchorSmoothScroll();
    initParticles();
    initSectionReveal();
    initStatsCounter();
    initFaq();
    initScrollTopButton();
    initParallax();
    initHeroLoadAnimations();

    window.addEventListener("scroll", initNavbarScrollState);

    console.log("%cMamaCare", "color: #1baebe; font-size: 22px; font-weight: 700;");
    console.log("%cMaking maternal care safer and easier", "color: #64748b; font-size: 13px;");
})();
