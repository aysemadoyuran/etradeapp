/** @format */

var THEMETAGS = THEMETAGS || {};
jQuery(function ($) {
  "use strict";

  //preloader
  $(window).ready(function () {
    $("#preloader").delay(100).fadeOut("fade");
  });

  //dropdown menu hover js
  $("ul.nav li.dropdown").hover(function () {
    $(this).find(".dropdown-menu").stop(true, true).delay(100).fadeIn(200);
  }, function () {
    $(this).find(".dropdown-menu").stop(true, true).delay(100).fadeOut(200);
  });

  //sticky header
  $(window).on("scroll", function () {
    var scroll = $(window).scrollTop();
    if (scroll < 2) {
      $("nav.sticky-header").removeClass("affix");
    } else {
      $("nav.sticky-header").addClass("affix");
    }
  });

  //swiper slide js
  var swiper = new Swiper(".testimonialSwiper", {
    slidesPerView: 1,
    speed: 700,
    spaceBetween: 30,
    slidesPerGroup: 1,
    loop: true,
    breakpoints: {
      320: {
        slidesPerView: 1
      },
      640: {
        slidesPerView: 1
      },
      768: {
        slidesPerView: 2,
        spaceBetween: 20
      },
      1024: {
        slidesPerView: 2,
        spaceBetween: 20
      },
      1142: {
        slidesPerView: 2,
        spaceBetween: 25
      }
    },
    navigation: {
      nextEl: ".swiper-button-next",
      prevEl: ".swiper-button-prev"
    }
  });
  //swiper slide js
  var swiper = new Swiper(".testimonialThreeSwiper", {
    slidesPerView: 2,
    speed: 700,
    spaceBetween: 30,
    slidesPerGroup: 2,
    loop: true,
    pagination: {
      el: ".swiper-pagination",
      clickable: true
    },
    breakpoints: {
      320: {
        slidesPerView: 1,
        spaceBetween: 30
      },
      768: {
        slidesPerView: 2,
        spaceBetween: 30
      },
      991: {
        slidesPerView: 3,
        spaceBetween: 30
      }
    },
    navigation: {
      nextEl: ".swiper-button-next",
      prevEl: ".swiper-button-prev"
    }
  });
  //app two review slider
  var swiper = new Swiper(".appTwoReviewSwiper", {
    slidesPerView: 2,
    speed: 700,
    spaceBetween: 30,
    slidesPerGroup: 2,
    loop: true,
    navigation: {
      nextEl: ".swiper-button-next",
      prevEl: ".swiper-button-prev"
    },
    breakpoints: {
      320: {
        slidesPerView: 1,
        spaceBetween: 30
      },
      768: {
        slidesPerView: 2,
        spaceBetween: 30
      },
      991: {
        slidesPerView: 3,
        spaceBetween: 30
      }
    }
  });
  // Goal Slider
  const goalSlider = document.querySelector(".ca-goal-slider");
  if (goalSlider) {
    const goalSliderInit = new Swiper(goalSlider, {
      loop: true,
      slidesPerView: 1,
      spaceBetween: 24,
      breakpoints: {
        768: {
          slidesPerView: 2
        },
        1200: {
          slidesPerView: 3
        }
      },
      pagination: {
        el: ".ca-goal-slider__pagination",
        clickable: true
      }
    });
  }
  // Testimonial Slider
  const testimonialSlider = document.querySelector(".ca-testimonial-slider");
  if (testimonialSlider) {
    const testimonialSliderInit = new Swiper(testimonialSlider, {
      loop: true,
      slidesPerView: 1,
      spaceBetween: 24,
      breakpoints: {
        768: {
          slidesPerView: 2
        },
        1200: {
          slidesPerView: 3
        }
      },
      pagination: {
        el: ".ca-testimonial-slider__pagination",
        clickable: true
      }
    });
  }
  // Client Slider
  const clientSlider = document.querySelector(".ca-client-slider");
  if (clientSlider) {
    const clientSliderInit = new Swiper(clientSlider, {
      loop: true,
      slidesPerView: 1,
      spaceBetween: 24,
      speed: 8000,
      autoplay: {
        delay: 1,
        disableOnInteraction: false
      },
      breakpoints: {
        576: {
          slidesPerView: 2
        },
        768: {
          slidesPerView: 3
        },
        992: {
          slidesPerView: 4
        },
        1200: {
          slidesPerView: 5
        }
      }
    });
  }
  // Feedback Slider
  const feedback = document.querySelector(".ca-client-feedback");
  if (feedback) {
    const feedbackInit = new Swiper(feedback, {
      loop: true,
      slidesPerView: 1,
      spaceBetween: 24,
      speed: 1000,
      autoplay: true,
      pagination: {
        el: ".ca-client-feedback__pagination"
      }
    });
  }
  //logo
  var swiper = new Swiper(".brand-logo-slider", {
    slidesPerView: 2,
    speed: 700,
    spaceBetween: 30,
    slidesPerGroup: 2,
    loop: true,
    breakpoints: {
      320: {
        slidesPerView: 2,
        spaceBetween: 30
      },
      768: {
        slidesPerView: 3,
        spaceBetween: 30
      },
      991: {
        slidesPerView: 5,
        spaceBetween: 30
      }
    }
  });
  //testimonial cyber
  var swiper = new Swiper(".cyber-testimonial-slider", {
    slidesPerView: 1,
    speed: 700,
    pagination: {
      clickable: true,
      el: ".swiper-pagination"
    },
    slidesPerGroup: 2,
    loop: true,
    breakpoints: {
      320: {
        slidesPerView: 1,
        spaceBetween: 30
      },
      768: {
        slidesPerView: 1
      },
      991: {
        slidesPerView: 1
      }
    }
  });
  //cyber blog
  var swiper = new Swiper(".cyber-blog", {
    slidesPerView: 3,
    spaceBetween: 30,
    speed: 1000,
    autoplay: {
      delay: 2500
    },
    slidesPerGroup: 1,
    loop: true,
    breakpoints: {
      320: {
        slidesPerView: 1,
        spaceBetween: 30
      },
      768: {
        slidesPerView: 2
      },
      991: {
        slidesPerView: 3
      }
    }
  });
  //crypto testimonial blog
  var swiper = new Swiper(".crypto-testimonial", {
    slidesPerView: 3,
    spaceBetween: 30,
    speed: 1000,
    autoplay: {
      delay: 2500
    },
    slidesPerGroup: 1,
    loop: true,
    breakpoints: {
      320: {
        slidesPerView: 1,
        spaceBetween: 30
      },
      768: {
        slidesPerView: 2
      },
      991: {
        slidesPerView: 3
      }
    }
  });
  //hero-slider
  var swiper = new Swiper(".hero-slider", {
    slidesPerView: 1,
    loop: true,
    speed: 800,
    autoplay: {
      delay: 3000
    }
  });

  //payment brand logo
  var swiper = new Swiper(".payment-brand-logo", {
    slidesPerView: 1,
    spaceBetween: 24,
    speed: 1000,
    autoplay: {
      delay: 2500
    },
    slidesPerGroup: 1,
    loop: true,
    breakpoints: {
      320: {
        slidesPerView: 1,
        spaceBetween: 16
      },
      768: {
        slidesPerView: 3
      },
      991: {
        slidesPerView: 5
      }
    }
  });
  //payment testimonial
  var swiper = new Swiper(".payment-testimonial-slider", {
    slidesPerView: 2,
    spaceBetween: 24,
    speed: 1000,
    autoplay: {
      delay: 3000
    },
    slidesPerGroup: 1,
    loop: true,
    breakpoints: {
      320: {
        slidesPerView: 1
      },
      768: {
        slidesPerView: 2
      },
      991: {
        slidesPerView: 3
      }
    }
  });

  //digital marketing brand logo logo
  var swiper = new Swiper(".digi-logo-slider", {
    slidesPerView: 1,
    spaceBetween: 24,
    speed: 1000,
    autoplay: {
      delay: 2500
    },
    slidesPerGroup: 1,
    loop: true,
    breakpoints: {
      320: {
        slidesPerView: 2,
        spaceBetween: 10
      },
      768: {
        slidesPerView: 3
      },
      992: {
        slidesPerView: 6
      }
    }
  });

  //digital marketing testimonial
  var swiper = new Swiper(".digi-testimonial-wrapper", {
    slidesPerView: 1,
    loop: true,
    speed: 800,
    navigation: {
      nextEl: ".digi-nav-control .swiper-button-next",
      prevEl: ".digi-nav-control .swiper-button-prev"
    }
  });
  var swiper = new Swiper(".ai-wi-testimonial", {
    slidesPerView: 1,
    loop: true,
    speed: 800
  });

  // Home 35 Feedback
  const feedbackSlider35 = document.querySelector(".feedback-slider");
  if (feedbackSlider35) {
    new Swiper(feedbackSlider35, {
      loop: true,
      centeredSlides: true,
      slidesPerView: 1,
      centeredSlidesBounds: true,
      speed: 5000,
      autoplay: {
        delay: 1,
        disableOnInteraction: false
      },
      breakpoints: {
        768: {
          slidesPerView: 2,
          spaceBetween: 24
        },
        992: {
          slidesPerView: 3,
          spaceBetween: 24
        },
        1920: {
          slidesPerView: 5,
          spaceBetween: 24
        }
      }
    });
  }
  var swiper = new Swiper(".ai-wt-enc-slider-1", {
    loop: true,
    slidesPerView: 4,
    centeredSlides: true,
    centeredSlidesBounds: true,
    speed: 5000,
    autoplay: {
      delay: 1,
      disableOnInteraction: false
    },
    breakpoints: {
      0: {
        slidesPerView: 3,
        centeredSlides: false
      },
      767: {
        slidesPerView: 4,
        centeredSlides: true
      },
      768: {
        slidesPerView: 6,
        centeredSlides: true
      },
      991: {
        slidesPerView: 6,
        centeredSlides: true
      },
      992: {
        slidesPerView: 8,
        centeredSlides: true
      },
      1199: {
        slidesPerView: 8,
        centeredSlides: true
      },
      1399: {
        slidesPerView: 9,
        centeredSlides: true
      },
      1400: {
        slidesPerView: 4,
        centeredSlides: true
      }
    }
  });
  var swiper = new Swiper(".ai-wt-enc-slider-2", {
    loop: true,
    slidesPerView: 4,
    centeredSlides: true,
    centeredSlidesBounds: true,
    speed: 5000,
    autoplay: {
      delay: 1,
      disableOnInteraction: false,
      reverseDirection: true
    },
    breakpoints: {
      0: {
        slidesPerView: 3,
        centeredSlides: false
      },
      767: {
        slidesPerView: 4,
        centeredSlides: true
      },
      768: {
        slidesPerView: 6,
        centeredSlides: true
      },
      991: {
        slidesPerView: 6,
        centeredSlides: true
      },
      992: {
        slidesPerView: 8,
        centeredSlides: true
      },
      1199: {
        slidesPerView: 8,
        centeredSlides: true
      },
      1399: {
        slidesPerView: 9,
        centeredSlides: true
      },
      1400: {
        slidesPerView: 4,
        centeredSlides: true
      }
    }
  });

  // Feedback 38
  var feedback38Slider = new Swiper(".feedback-38-slider", {
    slidesPerView: 3,
    spaceBetween: 30,
    speed: 1000,
    autoplay: {
      delay: 2500
    },
    slidesPerGroup: 1,
    loop: true,
    navigation: {
      nextEl: ".swiper-button-next",
      prevEl: ".swiper-button-prev"
    },
    breakpoints: {
      320: {
        slidesPerView: 1,
        spaceBetween: 30
      },
      992: {
        slidesPerView: 2
      },
      1200: {
        slidesPerView: 3
      }
    }
  });

  // Team 39
  var team_39 = new Swiper(".team_39", {
    spaceBetween: 24,
    speed: 1000,
    autoplay: {
      delay: 2500
    },
    slidesPerGroup: 1,
    loop: true,
    navigation: {
      nextEl: ".swiper-button-next",
      prevEl: ".swiper-button-prev"
    },
    breakpoints: {
      768: {
        slidesPerView: 2,
        spaceBetween: 30
      },
      992: {
        slidesPerView: 3
      },
      1200: {
        slidesPerView: 5
      }
    }
  });

  // Feedback 39
  var feedback_39_slider = new Swiper(".feedback_39_slider", {
    spaceBetween: 24,
    speed: 1000,
    autoplay: {
      delay: 2500
    },
    slidesPerGroup: 1,
    loop: true,
    navigation: {
      nextEl: ".swiper-button-next",
      prevEl: ".swiper-button-prev"
    },
    breakpoints: {
      768: {
        slidesPerView: 2,
        spaceBetween: 30
      },
      992: {
        slidesPerView: 3
      },
      1200: {
        slidesPerView: 4
      }
    }
  });

  // imgae infinite slider
  const scrollers = document.querySelectorAll(".scroller-x");
  scrollers.forEach(scroller => {
    scroller.setAttribute("data-animated", true);
    const scrollerInner = scroller.querySelector(".scroller-x__list");
    const scrollerContent = Array.from(scrollerInner.children);
    scrollerContent.forEach(item => {
      const duplicatedItem = item.cloneNode(true);
      duplicatedItem.setAttribute("aria-hidden", true);
      scrollerInner.appendChild(duplicatedItem);
    });
  });

  // ScrollerY
  const scrollersY = document.querySelectorAll(".scroller-y");
  scrollersY.forEach(scroller => {
    scroller.setAttribute("data-animated", true);
    const scrollerInner = scroller.querySelector(".scroller-y__list");
    const scrollerContent = Array.from(scrollerInner.children);
    scrollerContent.forEach(item => {
      const duplicatedItem = item.cloneNode(true);
      duplicatedItem.setAttribute("aria-hidden", true);
      scrollerInner.appendChild(duplicatedItem);
    });
  });
  // ScrollerY

  //digital marketing testimonial
  var swiper = new Swiper(".ev-testimonial-slider", {
    slidesPerView: 1,
    loop: true,
    speed: 800,
    pagination: {
      clickable: true,
      el: ".eve-pagination.swiper-pagination"
    }
  });

  //insurance gallery slider
  var swiper = new Swiper(".ins-gallery-slider", {
    slidesPerView: 3,
    loop: true,
    speed: 800,
    spaceBetween: 24,
    navigation: {
      nextEl: ".swiper-btn-next",
      prevEl: ".swiper-btn-prev"
    },
    breakpoints: {
      0: {
        slidesPerView: 1,
        centeredSlides: false
      },
      768: {
        slidesPerView: 3,
        centeredSlides: true
      }
    }
  });

  //insurance feedback slider
  var swiper = new Swiper(".ins-feedback-slider", {
    slidesPerView: 1,
    loop: true,
    speed: 800,
    autoplay: true,
    navigation: {
      nextEl: ".swiper-btn-next",
      prevEl: ".swiper-btn-prev"
    },
    breakpoints: {
      0: {
        spaceBetween: 40
      },
      1200: {
        spaceBetween: 90
      }
    }
  });

  //insurance brand slider
  var swiper = new Swiper(".ins-brand-slider", {
    slidesPerView: 4,
    loop: true,
    speed: 800,
    spaceBetween: 10,
    autoplay: true,
    breakpoints: {
      0: {
        slidesPerView: 2
      },
      575: {
        slidesPerView: 3
      },
      1200: {
        slidesPerView: 4
      }
    }
  });

  //Ca Two Slider
  var swiper = new Swiper(".ca-two-top-slider", {
    slidesPerView: 6,
    loop: true,
    speed: 1000,
    spaceBetween: 10,
    autoplay: true,
    breakpoints: {
      0: {
        slidesPerView: 2
      },
      720: {
        slidesPerView: 5
      },
      1200: {
        slidesPerView: 6
      }
    }
  });
  var crmTotalSlide = $(".crm-htw-slider .swiper-wrapper .crm-htw-single").length;
  if (crmTotalSlide < 10) {
    $(".crm_total_value").text("0" + crmTotalSlide);
  } else {
    $(".crm_total_value").text(crmTotalSlide);
  }

  //crm htw slider
  var swiper = new Swiper(".crm-htw-slider", {
    slidesPerView: 2,
    loop: true,
    speed: 800,
    spaceBetween: 24,
    autoplay: true,
    pagination: {
      el: ".swiper-pagination",
      type: "progressbar",
      clickable: true
    },
    breakpoints: {
      0: {
        slidesPerView: 1
      },
      768: {
        slidesPerView: 2
      },
      992: {
        slidesPerView: 1
      },
      1200: {
        slidesPerView: 2
      }
    }
  });
  var crmFeedbackSlider = new Swiper(".crm-feedback-slider", {
    spaceBetween: 10,
    autoplay: true,
    speed: 800,
    navigation: {
      nextEl: ".crm-next-control",
      prevEl: ".crm-prev-control"
    },
    loop: true,
    loopedSlides: 4
  });
  var crmFeedbackThumbSlider = new Swiper(".crm-feedback-control-slider", {
    spaceBetween: 0,
    centeredSlides: true,
    slidesPerView: 5,
    touchRatio: 0.2,
    slideToClickedSlide: true,
    loop: true,
    speed: 800,
    loopedSlides: 4,
    effect: "coverflow",
    coverflowEffect: {
      rotate: 0,
      slideShadows: false,
      scale: 1,
      stretch: 12,
      depth: 185
    }
  });
  crmFeedbackSlider.controller.control = crmFeedbackThumbSlider;
  crmFeedbackThumbSlider.controller.control = crmFeedbackSlider;
  var dgPortfolioSlider = new Swiper(".dg-portfolio-slider", {
    spaceBetween: 24,
    pagination: {
      el: ".dg-portfolio-slider-control",
      type: "bullets",
      clickable: true
    },
    breakpoints: {
      0: {
        slidesPerView: 1
      },
      768: {
        slidesPerView: 2
      },
      1200: {
        slidesPerView: 3
      }
    }
  });
  var dgTeamSlider = new Swiper(".dg-team-slider", {
    spaceBetween: 24,
    loop: true,
    pagination: {
      el: ".dg-team-slider-controls",
      type: "bullets",
      clickable: true
    },
    breakpoints: {
      0: {
        slidesPerView: 1
      },
      425: {
        slidesPerView: 2
      },
      768: {
        slidesPerView: 3
      },
      992: {
        slidesPerView: 2
      },
      1400: {
        slidesPerView: 3
      }
    }
  });
  var dgFeedbackSlider = new Swiper(".dg-feedback-slider", {
    spaceBetween: 24,
    autoplay: true,
    speed: 700,
    loop: true,
    breakpoints: {
      0: {
        slidesPerView: 1
      },
      768: {
        slidesPerView: 2
      },
      1200: {
        slidesPerView: 3
      }
    }
  });
  var scFeedbackSlider = new Swiper(".sc-feedback-slider", {
    autoplay: true,
    loop: true,
    speed: 700,
    spaceBetween: 24,
    centeredSlides: false,
    pagination: {
      el: ".itc-testimonial-slide-dots",
      clickable: true
    },
    breakpoints: {
      0: {
        slidesPerView: 1
      },
      768: {
        slidesPerView: 2
      },
      1200: {
        slidesPerView: 3
      }
    }
  });
  var caTwoProductSlider = new Swiper(".ca-two-product-slider", {
    autoplay: true,
    loop: true,
    speed: 700,
    spaceBetween: 24,
    breakpoints: {
      0: {
        slidesPerView: 1
      },
      768: {
        slidesPerView: 2
      },
      1200: {
        slidesPerView: 3
      },
      1600: {
        slidesPerView: 6
      }
    }
  });
  var caTwoMinTestimonial = new Swiper(".ca-two-min-testimonial", {
    autoplay: true,
    loop: true,
    speed: 700,
    direction: "vertical",
    effect: "slide",
    breakpoints: {
      0: {
        slidesPerView: 3
      },
      768: {
        slidesPerView: 3
      },
      1200: {
        slidesPerView: 3
      }
    }
  });
  var caTwoServiceSliderArea = new Swiper(".ca-two-service-slider-area", {
    autoplay: true,
    loop: true,
    speed: 700,
    spaceBetween: 10,
    breakpoints: {
      0: {
        slidesPerView: 1
      },
      768: {
        slidesPerView: 2
      },
      1200: {
        slidesPerView: 3
      },
      1872: {
        slidesPerView: 5
      }
    }
  });
  var riskTestimonialItems = new Swiper(".risk-testimonial-items", {
    autoplay: true,
    loop: true,
    speed: 700,
    spaceBetween: 24,
    pagination: {
      el: ".risk-slider__pagination",
      clickable: true
    },
    breakpoints: {
      0: {
        slidesPerView: 1
      },
      768: {
        slidesPerView: 2
      },
      1200: {
        slidesPerView: 3
      }
    }
  });
  var aiartPostSlider = new Swiper(".aiart-post-slider", {
    autoplay: true,
    loop: true,
    speed: 700,
    spaceBetween: 24,
    navigation: {
      nextEl: ".aiart-swiper-button-next",
      prevEl: ".aiart-swiper-button-prev"
    },
    breakpoints: {
      0: {
        slidesPerView: 1
      },
      768: {
        slidesPerView: 2
      },
      1200: {
        slidesPerView: 3
      }
    }
  });
  var aihTestimonial = new Swiper(".aih-testimonial", {
    autoplay: true,
    loop: true,
    speed: 700,
    spaceBetween: 24,
    pagination: {
      el: ".risk-slider__pagination",
      clickable: true
    },
    breakpoints: {
      0: {
        slidesPerView: 1
      },
      768: {
        slidesPerView: 2
      },
      1200: {
        slidesPerView: 3
      }
    }
  });
  var itCompanyProjectSlider = new Swiper(".it-company-project-slider", {
    autoplay: true,
    loop: true,
    speed: 700,
    spaceBetween: 24,
    navigation: {
      nextEl: ".it-company-swiper-button-next",
      prevEl: ".it-company-swiper-button-prev"
    },
    pagination: {
      el: ".it-company-slider__pagination",
      clickable: true
    },
    breakpoints: {
      0: {
        slidesPerView: 1
      },
      768: {
        slidesPerView: 2
      },
      992: {
        slidesPerView: 3
      },
      1200: {
        slidesPerView: 5
      }
    }
  });
  const brandSlider = document.querySelector(".qty-brand-slider");
  if (brandSlider) {
    const brandSliderInit = new Swiper(brandSlider, {
      loop: true,
      slidesPerView: 2,
      centeredSlides: true,
      centeredSlidesBounds: true,
      speed: 5000,
      spaceBetween: 16,
      autoplay: {
        delay: 1,
        disableOnInteraction: false
      },
      breakpoints: {
        576: {
          slidesPerView: 3
        },
        768: {
          slidesPerView: 4
        },
        992: {
          slidesPerView: 5
        },
        1200: {
          slidesPerView: 6
        },
        1400: {
          slidesPerView: 7
        },
        1600: {
          slidesPerView: 8
        },
        1920: {
          slidesPerView: 9
        }
      }
    });
  }
  var maHeroSlider = new Swiper(".ma-hero-slider", {
    loop: true,
    spaceBetween: 10,
    autoplay: true,
    speed: 800,
    navigation: {
      nextEl: ".crm-next-control",
      prevEl: ".crm-prev-control"
    }
  });

  // Hero 40 Slider
  var heroSlider40 = new Swiper(".hero_40_slider", {
    autoplay: true,
    loop: true,
    speed: 700,
    spaceBetween: 24,
    pagination: {
      el: ".risk-slider__pagination",
      clickable: true
    },
    breakpoints: {
      0: {
        slidesPerView: 1
      }
    }
  });

  // Aih Price
  var selectedBtn = document.querySelectorAll(".select-pricing-btn");
  if (selectedBtn) {
    selectedBtn.forEach(function (item) {
      item.addEventListener("click", function () {
        // Add "active" class to the clicked item
        item.classList.add("active");

        // Remove "active" class from sibling items
        selectedBtn.forEach(function (sibling) {
          if (sibling !== item) {
            sibling.classList.remove("active");
          }
        });
      });
    });
  }
  const selectMontlyPricing = document.querySelector("#selectMonthly");
  const selectYearlPricing = document.querySelector("#selectYearly");
  const monthlyPricing = document.querySelectorAll(".qty-price-card__price-monthly");
  const yearlyPricing = document.querySelectorAll(".qty-price-card__price-yearly");
  if (selectYearlPricing) {
    selectYearlPricing.addEventListener("click", () => {
      monthlyPricing.forEach(e => {
        e.style.cssText = `display: none;`;
      });
      yearlyPricing.forEach(e => {
        e.style.cssText = `display: flex;`;
      });
    });
    selectMontlyPricing.addEventListener("click", () => {
      monthlyPricing.forEach(e => {
        e.style.cssText = `display: flex;`;
      });
      yearlyPricing.forEach(e => {
        e.style.cssText = `display: none;`;
      });
    });
  }
  // Aih Price

  //slide controls
  $(".crm-next-control-outer").on("click", function () {
    $(".crm-next-control").trigger("click");
  });
  $(".crm-prev-control-outer").on("click", function () {
    $(".crm-prev-control").trigger("click");
  });

  //mk accordion
  $(".mk-accordion").each(function () {
    var accordionButton = $(this).find(".accordion-button");
    accordionButton.each(function () {
      $(this).on("click", function () {
        $(this).parents(".mk-accordion").find(".accordion-item.active").removeClass("active");
        $(this).parents(".accordion-item").addClass("active");
      });
    });
  });

  //mk pricing plan
  $(".mk-pricing-control li a").each(function () {
    $(this).on("click", function () {
      $(this).parents(".mk-pricing-control").find("a.active").removeClass("active");
      $(this).addClass("active");
    });
  });
  $(".mk-pricing-control .mk_monthly_switch").on("click", function () {
    $(".mk_monthly_price").show();
    $(".mk_yearly_price").hide();
    return false;
  });
  $(".mk-pricing-control .mk_yearly_switch").on("click", function () {
    $(".mk_monthly_price").hide();
    $(".mk_yearly_price").show();
    return false;
  });

  // Price ai

  $(".aiart-price-btn").on("click", function () {
    $(this).toggleClass("clicked");
    $(".aiart-month").toggleClass("hide");
    $(".aiart-year").toggleClass("show");
    return false;
  });

  //mk feedback slider
  var mkCarousel = $("#mkCarousel");
  $("#mkCarouselControl button").each(function () {
    $(this).on("click", function () {
      $(this).parent().find("button.active").removeClass("active");
      $(this).addClass("active");
    });
  });
  mkCarousel.on("slide.bs.carousel", function () {
    var findNumber = mkCarousel.find(".active").index();
    findNumber = findNumber + 2;
    var totalSlides = $("#mkCarousel .carousel-item").length;
    if (findNumber > totalSlides) {
      findNumber = 1;
    }
    $("#mkCarouselControl").find("button.active").removeClass("active");
    $("#mkCarouselControl button:nth-child(" + findNumber + ")").addClass("active");
  });
  $(".crm-monthly").on("click", function () {
    $(".crm-checkbox-switch").prop("checked", false);
  });
  $(".crm-yearly").on("click", function () {
    $(".crm-checkbox-switch").prop("checked", true);
  });
  $(".crm-pricing-switch").on("click", function () {
    var isBoxChecked = $(".crm-checkbox-switch").is(":checked");
    if (isBoxChecked !== true) {
      $(".crm_monthly_price").show();
      $(".crm_yearly_price").hide();
    } else {
      $(".crm_yearly_price").show();
      $(".crm_monthly_price").hide();
    }
  });

  //hd accordion
  $(".hd-accordion .accordion-header a").each(function () {
    $(this).on("click", function () {
      $(this).parents(".hd-accordion").find(".accordion-item.active").removeClass("active");
      $(this).parents(".accordion-item").addClass("active");
    });
  });

  //sc pricing component
  if ($(".sc-pricing-switch").length) {
    var pricingSwitchBtn = $(".sc-pricing-switch");
    var dataActiveValue = pricingSwitchBtn.find("button.active").data("value");
    if (dataActiveValue == "monthly") {
      $(".sc-pricing-column").find(".pricing-amount.yearly").hide();
      $(".sc-pricing-column").find(".pricing-amount.monthly").show();
    } else if (dataActiveValue == "yearly") {
      $(".sc-pricing-column").find(".pricing-amount.yearly").show();
      $(".sc-pricing-column").find(".pricing-amount.monthly").hide();
    }
    $(".sc-pricing-switch button").each(function () {
      $(this).on("click", function () {
        var dataActiveValue = $(this).data("value");
        $(this).parents(".sc-pricing-switch").find("button.active").removeClass("active");
        $(this).addClass("active");
        if (dataActiveValue == "monthly") {
          $(".sc-pricing-column").find(".pricing-amount.yearly").hide();
          $(".sc-pricing-column").find(".pricing-amount.monthly").show();
        } else if (dataActiveValue == "yearly") {
          $(".sc-pricing-column").find(".pricing-amount.yearly").show();
          $(".sc-pricing-column").find(".pricing-amount.monthly").hide();
        }
      });
    });
  }
  THEMETAGS.initialize = {
    init: function () {
      THEMETAGS.initialize.general();
    },
    general: function () {
      // Mouse Move Parallax Element
      var $scene = $(".parallax-element").parallax({
        scalarX: 100,
        scalarY: 100
      });
    }
  };
  THEMETAGS.documentOnReady = {
    init: function () {
      THEMETAGS.initialize.init();
    }
  };
  $(document).ready(THEMETAGS.documentOnReady.init);
  $(function () {
    $('[data-bs-toggle="tooltip"]').tooltip();
  });

  //animated js
  AOS.init({
    easing: "ease-in-out",
    // default easing for AOS animations
    once: true,
    // whether animation should happen only once - while scrolling down
    duration: 500 // values from 0 to 3000, with step 50ms
  });

  //magnific popup js
  $(".popup-youtube, .popup-vimeo, .popup-gmaps").magnificPopup({
    disableOn: 700,
    type: "iframe",
    mainClass: "mfp-fade",
    removalDelay: 160,
    preloader: false,
    fixedContentPos: false
  });
  $(".popup-with-form").magnificPopup({
    type: "inline",
    preloader: false,
    focus: "#name"
  });
  $(window).on("load", function () {
    $(".masonry_grid").masonry({
      itemSelector: ".grid_item",
      columnWidth: 1
    });
  });

  //    dark light mood
  var setDarkMode = (active = false) => {
    console.log(active);
    var wrapper = document.querySelector(":root");
    if (active) {
      wrapper.setAttribute("data-bs-theme", "dark");
      localStorage.setItem("theme", "dark");
    } else {
      wrapper.setAttribute("data-bs-theme", "light");
      localStorage.setItem("theme", "light");
    }
  };
  var toggleDarkMode = () => {
    var theme = document.querySelector(":root").getAttribute("data-bs-theme");
    // If the current theme is "light", we want to activate dark
    setDarkMode(theme === "light");
  };
  var initDarkMode = () => {
    var theme = localStorage.getItem("theme");
    if (theme == "dark") {
      setDarkMode(true);
    } else {
      setDarkMode(false);
    }
    var toggleButton = document.querySelector(".tt-theme-toggle");
    toggleButton && toggleButton.addEventListener("click", toggleDarkMode);
  };
  initDarkMode();
});